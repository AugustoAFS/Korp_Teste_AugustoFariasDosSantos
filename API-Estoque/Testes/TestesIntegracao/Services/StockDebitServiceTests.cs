using Estoque.ApplicationService.Services;
using Estoque.Domain.Dtos.EventListeners;
using Estoque.Domain.Entities;
using Estoque.EventListeners.Publishers;
using Estoque.InfraStructure.Data;
using Estoque.InfraStructure.Repositories;
using Estoque.TestesIntegracao.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Estoque.TestesIntegracao.Services;

[Collection(BancoCollection.Nome)]
public sealed class StockDebitServiceTests(SqlServerFixture banco) : IAsyncLifetime
{
    public async Task InitializeAsync() => await banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    private static StockDebitService Servico(EstoqueDbContext contexto)
    {
        var outbox = new OutboxRepository(contexto);

        return new StockDebitService(
            new ProductRepository(contexto),
            new StockMovementRepository(contexto),
            new ProcessedMessageRepository(contexto),
            new EstoqueEventPublisher(outbox),
            new UnitOfWork(contexto),
            NullLogger<StockDebitService>.Instance);
    }

    private async Task<Product> Semear(string codigo, int saldo)
    {
        await using var contexto = banco.CreateContext();
        var produto = new Product(codigo, $"Produto {codigo}", saldo);
        contexto.Products.Add(produto);
        await contexto.SaveChangesAsync();
        return produto;
    }

    private async Task<int> SaldoDe(Guid produtoId)
    {
        await using var contexto = banco.CreateContext();
        return await contexto.Products.AsNoTracking().Where(p => p.Id == produtoId).Select(p => p.Balance).FirstAsync();
    }

    private static DebitItem Item(Guid produto, int quantidade)
        => new() { ProductId = produto, Quantity = quantidade };

    #region Baixa concluída

    [Fact]
    public async Task Baixa_completa_debita_todos_os_produtos()
    {
        var a = await Semear("A", 10);
        var b = await Semear("B", 10);

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).DebitStock(42, Guid.CreateVersion7(), 7, [Item(a.Id, 2), Item(b.Id, 3)], default);

        (await SaldoDe(a.Id)).ShouldBe(8);
        (await SaldoDe(b.Id)).ShouldBe(7);
    }

    [Fact]
    public async Task Baixa_completa_enfileira_o_evento_de_sucesso_no_outbox()
    {
        var a = await Semear("A", 10);

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).DebitStock(42, Guid.CreateVersion7(), 7, [Item(a.Id, 1)], default);

        await using var conferencia = banco.CreateContext();
        var mensagens = await conferencia.OutboxMessages.AsNoTracking().ToListAsync();
        mensagens.ShouldContain(m => m.Type == "EstoqueBaixadoEvent");
    }

    [Fact]
    public async Task Baixa_completa_grava_um_movimento_de_saida_por_item()
    {
        var a = await Semear("A", 10);
        var b = await Semear("B", 10);

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).DebitStock(42, Guid.CreateVersion7(), 7, [Item(a.Id, 2), Item(b.Id, 3)], default);

        await using var conferencia = banco.CreateContext();
        (await conferencia.StockMovements.AsNoTracking().CountAsync()).ShouldBe(2);
    }

    #endregion

    #region Tudo-ou-nada com savepoint

    [Fact]
    public async Task Item_sem_saldo_desfaz_as_baixas_ja_aplicadas_na_mesma_nota()
    {
        var a = await Semear("A", 10);
        var b = await Semear("B", 1);

        await using (var contexto = banco.CreateContext())
        {
            var resultado = await Servico(contexto)
                .DebitStock(42, Guid.CreateVersion7(), 7, [Item(a.Id, 2), Item(b.Id, 99)], default);

            resultado.Success.ShouldBeFalse();
        }

        (await SaldoDe(a.Id)).ShouldBe(10);
        (await SaldoDe(b.Id)).ShouldBe(1);
    }

    [Fact]
    public async Task Rejeicao_preserva_o_marcador_de_idempotencia_apesar_do_rollback()
    {
        var b = await Semear("B", 1);
        var processamento = Guid.CreateVersion7();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).DebitStock(42, processamento, 7, [Item(b.Id, 99)], default);

        await using var conferencia = banco.CreateContext();
        var marcador = await conferencia.ProcessedMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MessageId == processamento);

        marcador.ShouldNotBeNull();
    }

    [Fact]
    public async Task Rejeicao_preserva_o_evento_de_rejeicao_apesar_do_rollback()
    {
        var b = await Semear("B", 1);

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).DebitStock(42, Guid.CreateVersion7(), 7, [Item(b.Id, 99)], default);

        await using var conferencia = banco.CreateContext();
        var mensagens = await conferencia.OutboxMessages.AsNoTracking().ToListAsync();

        mensagens.ShouldContain(m => m.Type == "EstoqueRejeitadoEvent");
    }

    [Fact]
    public async Task Rejeicao_nao_grava_movimento_de_estoque()
    {
        var a = await Semear("A", 10);
        var b = await Semear("B", 1);

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).DebitStock(42, Guid.CreateVersion7(), 7, [Item(a.Id, 2), Item(b.Id, 99)], default);

        await using var conferencia = banco.CreateContext();
        (await conferencia.StockMovements.AsNoTracking().CountAsync()).ShouldBe(0);
    }

    #endregion

    #region Duplicata reemite o desfecho — requisito opcional (c)

    [Fact]
    public async Task Duplicata_nao_debita_o_estoque_uma_segunda_vez()
    {
        var a = await Semear("A", 10);
        var processamento = Guid.CreateVersion7();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).DebitStock(42, processamento, 7, [Item(a.Id, 3)], default);

        await using (var repeticao = banco.CreateContext())
            await Servico(repeticao).DebitStock(42, processamento, 7, [Item(a.Id, 3)], default);

        (await SaldoDe(a.Id)).ShouldBe(7);
    }

    [Fact]
    public async Task Duplicata_reemite_o_evento_original_em_vez_de_engolir()
    {
        var a = await Semear("A", 10);
        var processamento = Guid.CreateVersion7();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).DebitStock(42, processamento, 7, [Item(a.Id, 3)], default);

        await using (var repeticao = banco.CreateContext())
            await Servico(repeticao).DebitStock(42, processamento, 7, [Item(a.Id, 3)], default);

        await using var conferencia = banco.CreateContext();
        var baixados = await conferencia.OutboxMessages.AsNoTracking()
            .CountAsync(m => m.Type == "EstoqueBaixadoEvent");

        baixados.ShouldBe(2);
    }

    [Fact]
    public async Task Duplicata_de_rejeicao_reemite_a_rejeicao()
    {
        var b = await Semear("B", 1);
        var processamento = Guid.CreateVersion7();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).DebitStock(42, processamento, 7, [Item(b.Id, 99)], default);

        await using (var repeticao = banco.CreateContext())
            await Servico(repeticao).DebitStock(42, processamento, 7, [Item(b.Id, 99)], default);

        await using var conferencia = banco.CreateContext();
        var rejeitados = await conferencia.OutboxMessages.AsNoTracking()
            .CountAsync(m => m.Type == "EstoqueRejeitadoEvent");

        rejeitados.ShouldBe(2);
    }

    [Fact]
    public async Task Duplicata_nao_duplica_o_movimento_de_estoque()
    {
        var a = await Semear("A", 10);
        var processamento = Guid.CreateVersion7();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).DebitStock(42, processamento, 7, [Item(a.Id, 3)], default);

        await using (var repeticao = banco.CreateContext())
            await Servico(repeticao).DebitStock(42, processamento, 7, [Item(a.Id, 3)], default);

        await using var conferencia = banco.CreateContext();
        (await conferencia.StockMovements.AsNoTracking().CountAsync()).ShouldBe(1);
    }

    #endregion

    #region Concorrência entre consumos

    [Fact]
    public async Task Dois_consumos_simultaneos_da_mesma_mensagem_debitam_uma_vez_so()
    {
        var a = await Semear("A", 10);
        var processamento = Guid.CreateVersion7();

        async Task Consumir()
        {
            await using var contexto = banco.CreateContext();
            try
            {
                await Servico(contexto).DebitStock(42, processamento, 7, [Item(a.Id, 3)], default);
            }
            catch (DbUpdateException)
            {
            }
        }

        await Task.WhenAll(Consumir(), Consumir());

        (await SaldoDe(a.Id)).ShouldBe(7);
    }

    #endregion
}
