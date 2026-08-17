using Estoque.Domain.Entities;
using Estoque.Domain.Enums;
using Shouldly;

namespace Estoque.TestesUnitarios.Entities;

public sealed class StockMovementTests
{
    [Fact]
    public void Outbound_deriva_o_saldo_anterior_somando_a_quantidade_de_volta()
    {
        var movimento = StockMovement.Outbound(Guid.CreateVersion7(), 2, 8, 100, Guid.CreateVersion7(), 7);

        movimento.Type.ShouldBe(MovementType.Outbound);
        movimento.Quantity.ShouldBe(2);
        movimento.BalanceAfter.ShouldBe(8);
        movimento.BalanceBefore.ShouldBe(10);
    }

    [Fact]
    public void Outbound_guarda_a_nota_a_chave_de_idempotencia_e_o_usuario()
    {
        var produtoId = Guid.CreateVersion7();
        var chave = Guid.CreateVersion7();

        var movimento = StockMovement.Outbound(produtoId, 3, 5, 42, chave, 7);

        movimento.ProductId.ShouldBe(produtoId);
        movimento.InvoiceId.ShouldBe(42);
        movimento.IdempotencyKey.ShouldBe(chave);
        movimento.MovedByUserId.ShouldBe(7);
    }

    [Fact]
    public void Inbound_parte_do_zero_e_nao_tem_nota_nem_chave()
    {
        var movimento = StockMovement.Inbound(Guid.CreateVersion7(), 15, 7);

        movimento.Type.ShouldBe(MovementType.Inbound);
        movimento.Quantity.ShouldBe(15);
        movimento.BalanceBefore.ShouldBe(0);
        movimento.BalanceAfter.ShouldBe(15);
        movimento.InvoiceId.ShouldBeNull();
        movimento.IdempotencyKey.ShouldBeNull();
    }

    [Fact]
    public void Movimento_registra_o_instante_da_ocorrencia()
    {
        var antes = DateTimeOffset.UtcNow;

        var movimento = StockMovement.Inbound(Guid.CreateVersion7(), 1, null);

        movimento.OccurredAt.ShouldBeInRange(antes, DateTimeOffset.UtcNow);
    }
}
