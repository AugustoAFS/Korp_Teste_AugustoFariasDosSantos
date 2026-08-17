using Estoque.Domain.Entities;
using Estoque.Domain.Enums;
using Estoque.InfraStructure.Repositories;
using Estoque.TestesIntegracao.Suporte;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Estoque.TestesIntegracao.Repositories;

[Collection(BancoCollection.Nome)]
public sealed class StockMovementRepositoryTests(SqlServerFixture banco) : IAsyncLifetime
{
    public async Task InitializeAsync() => await banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SemearProduto(string codigo = "PAR-M8", int saldo = 10)
    {
        await using var contexto = banco.CreateContext();
        var produto = new Product(codigo, $"Produto {codigo}", saldo);
        contexto.Products.Add(produto);
        await contexto.SaveChangesAsync();
        return produto.Id;
    }

    [Fact]
    public async Task Movimento_de_entrada_e_persistido_com_o_saldo_resultante()
    {
        var produtoId = await SemearProduto();

        await using (var contexto = banco.CreateContext())
        {
            await new StockMovementRepository(contexto)
                .AddRange([StockMovement.Inbound(produtoId, 15, 7)], default);
            await contexto.SaveChangesAsync();
        }

        await using var conferencia = banco.CreateContext();
        var movimento = await conferencia.StockMovements.AsNoTracking().SingleAsync();

        movimento.Type.ShouldBe(MovementType.Inbound);
        movimento.Quantity.ShouldBe(15);
        movimento.BalanceAfter.ShouldBe(15);
    }

    [Fact]
    public async Task Movimento_de_saida_guarda_a_nota_e_a_chave_de_idempotencia()
    {
        var produtoId = await SemearProduto();
        var chave = Guid.CreateVersion7();

        await using (var contexto = banco.CreateContext())
        {
            await new StockMovementRepository(contexto)
                .AddRange([StockMovement.Outbound(produtoId, 2, 8, 42, chave, 7)], default);
            await contexto.SaveChangesAsync();
        }

        await using var conferencia = banco.CreateContext();
        var movimento = await conferencia.StockMovements.AsNoTracking().SingleAsync();

        movimento.InvoiceId.ShouldBe(42);
        movimento.IdempotencyKey.ShouldBe(chave);
        movimento.BalanceBefore.ShouldBe(10);
        movimento.BalanceAfter.ShouldBe(8);
    }

    [Fact]
    public async Task Lote_de_movimentos_e_gravado_de_uma_vez()
    {
        var primeiro = await SemearProduto("A");
        var segundo = await SemearProduto("B");
        var chave = Guid.CreateVersion7();

        await using (var contexto = banco.CreateContext())
        {
            await new StockMovementRepository(contexto).AddRange(
                [
                    StockMovement.Outbound(primeiro, 1, 9, 42, chave, 7),
                    StockMovement.Outbound(segundo, 2, 8, 42, chave, 7)
                ],
                default);
            await contexto.SaveChangesAsync();
        }

        await using var conferencia = banco.CreateContext();
        (await conferencia.StockMovements.AsNoTracking().CountAsync()).ShouldBe(2);
    }

    [Fact]
    public async Task Movimento_exige_um_produto_existente()
    {
        await using var contexto = banco.CreateContext();

        await new StockMovementRepository(contexto)
            .AddRange([StockMovement.Inbound(Guid.CreateVersion7(), 1, 7)], default);

        await Should.ThrowAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task Mesma_chave_de_idempotencia_no_mesmo_produto_e_recusada()
    {
        var produtoId = await SemearProduto();
        var chave = Guid.CreateVersion7();

        await using (var contexto = banco.CreateContext())
        {
            await new StockMovementRepository(contexto)
                .AddRange([StockMovement.Outbound(produtoId, 1, 9, 42, chave, 7)], default);
            await contexto.SaveChangesAsync();
        }

        await using var repetido = banco.CreateContext();
        await new StockMovementRepository(repetido)
            .AddRange([StockMovement.Outbound(produtoId, 1, 8, 42, chave, 7)], default);

        await Should.ThrowAsync<DbUpdateException>(() => repetido.SaveChangesAsync());
    }
}
