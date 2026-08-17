using System.Text.Json;
using Estoque.Domain.Dtos.EventListeners;
using Estoque.EventListeners.Messages.Publicados;
using Estoque.EventListeners.Publishers;
using Estoque.InfraStructure.Repositories;
using Estoque.TestesIntegracao.Suporte;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Estoque.TestesIntegracao.EventListeners.Publishers;

[Collection(BancoCollection.Nome)]
public sealed class EstoqueEventPublisherTests(SqlServerFixture banco) : IAsyncLifetime
{
    public async Task InitializeAsync() => await banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(string Tipo, string Payload)> Publicar(
        Func<EstoqueEventPublisher, Task<StoredEvent>> acao)
    {
        await using var contexto = banco.CreateContext();
        var evento = await acao(new EstoqueEventPublisher(new OutboxRepository(contexto)));
        await contexto.SaveChangesAsync();

        var gravada = await contexto.OutboxMessages.AsNoTracking().SingleAsync();
        gravada.Type.ShouldBe(evento.Type);
        gravada.Payload.ShouldBe(evento.Payload);

        return (gravada.Type, gravada.Payload);
    }

    [Fact]
    public async Task Produto_criado_vai_para_o_outbox_e_nao_direto_para_o_broker()
    {
        var produtoId = Guid.CreateVersion7();

        var (tipo, payload) = await Publicar(publisher =>
            publisher.PublishProductCreated(produtoId, "PAR-M8", "Parafuso sextavado M8", true, default));

        tipo.ShouldBe(nameof(ProdutoCriadoEvent));

        var evento = JsonSerializer.Deserialize<ProdutoCriadoEvent>(payload)!;
        evento.ProdutoId.ShouldBe(produtoId);
        evento.Codigo.ShouldBe("PAR-M8");
        evento.Ativo.ShouldBeTrue();
    }

    [Fact]
    public async Task Produto_atualizado_leva_a_situacao_para_o_faturamento()
    {
        var (tipo, payload) = await Publicar(publisher =>
            publisher.PublishProductUpdated(Guid.CreateVersion7(), "PAR-M8", "Parafuso", false, default));

        tipo.ShouldBe(nameof(ProdutoAtualizadoEvent));
        JsonSerializer.Deserialize<ProdutoAtualizadoEvent>(payload)!.Ativo.ShouldBeFalse();
    }

    [Fact]
    public async Task Estoque_baixado_carrega_os_saldos_resultantes()
    {
        var produtoId = Guid.CreateVersion7();
        var processamento = Guid.CreateVersion7();

        var (tipo, payload) = await Publicar(publisher => publisher.PublishStockDebited(
            42, processamento, [new UpdatedBalance { ProductId = produtoId, NewBalance = 8 }], default));

        tipo.ShouldBe(nameof(EstoqueBaixadoEvent));

        var evento = JsonSerializer.Deserialize<EstoqueBaixadoEvent>(payload)!;
        evento.NotaFiscalId.ShouldBe(42);
        evento.ProcessamentoId.ShouldBe(processamento);
        evento.Itens.Single().ProdutoId.ShouldBe(produtoId);
    }

    [Fact]
    public async Task Estoque_rejeitado_carrega_o_produto_e_o_motivo()
    {
        var produtoId = Guid.CreateVersion7();

        var (tipo, payload) = await Publicar(publisher => publisher.PublishStockRejected(
            42, Guid.CreateVersion7(), produtoId, "Saldo insuficiente.", default));

        tipo.ShouldBe(nameof(EstoqueRejeitadoEvent));

        var evento = JsonSerializer.Deserialize<EstoqueRejeitadoEvent>(payload)!;
        evento.ProdutoId.ShouldBe(produtoId);
        evento.Motivo.ShouldBe("Saldo insuficiente.");
    }

    [Fact]
    public async Task Republish_reenfileira_o_desfecho_guardado_sem_alterar_nada()
    {
        var guardado = new StoredEvent
        {
            Type = nameof(EstoqueBaixadoEvent),
            Payload = """{"NotaFiscalId":42,"ProcessamentoId":"00000000-0000-0000-0000-000000000001","Itens":[]}"""
        };

        await using var contexto = banco.CreateContext();
        await new EstoqueEventPublisher(new OutboxRepository(contexto)).Republish(guardado, default);
        await contexto.SaveChangesAsync();

        var gravada = await contexto.OutboxMessages.AsNoTracking().SingleAsync();
        gravada.Type.ShouldBe(guardado.Type);
        gravada.Payload.ShouldBe(guardado.Payload);
    }

    [Fact]
    public async Task Tipo_gravado_no_outbox_bate_com_o_mapa_do_dispatcher()
    {
        await using var contexto = banco.CreateContext();
        var publisher = new EstoqueEventPublisher(new OutboxRepository(contexto));

        await publisher.PublishProductCreated(Guid.CreateVersion7(), "A", "B", true, default);
        await publisher.PublishProductUpdated(Guid.CreateVersion7(), "A", "B", true, default);
        await publisher.PublishStockDebited(1, Guid.CreateVersion7(), [], default);
        await publisher.PublishStockRejected(1, Guid.CreateVersion7(), Guid.CreateVersion7(), "x", default);
        await contexto.SaveChangesAsync();

        var tipos = await contexto.OutboxMessages.AsNoTracking().Select(m => m.Type).ToListAsync();

        tipos.ShouldBe(
            [
                nameof(ProdutoCriadoEvent),
                nameof(ProdutoAtualizadoEvent),
                nameof(EstoqueBaixadoEvent),
                nameof(EstoqueRejeitadoEvent)
            ],
            ignoreOrder: true);
    }
}
