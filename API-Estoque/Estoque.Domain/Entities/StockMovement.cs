using Estoque.Domain.Enums;

namespace Estoque.Domain.Entities;

public sealed class StockMovement
{
    private StockMovement() { }

    private StockMovement(
        Guid productId,
        MovementType type,
        int quantity,
        int balanceBefore,
        int balanceAfter,
        long? invoiceId,
        Guid? idempotencyKey,
        long? movedByUserId)
    {
        Id = Guid.CreateVersion7();
        ProductId = productId;
        Type = type;
        Quantity = quantity;
        BalanceBefore = balanceBefore;
        BalanceAfter = balanceAfter;
        InvoiceId = invoiceId;
        IdempotencyKey = idempotencyKey;
        MovedByUserId = movedByUserId;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public MovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public int BalanceBefore { get; private set; }
    public int BalanceAfter { get; private set; }
    public long? InvoiceId { get; private set; }
    public Guid? IdempotencyKey { get; private set; }
    public long? MovedByUserId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    public static StockMovement Outbound(
        Guid productId,
        int quantity,
        int balanceAfter,
        long invoiceId,
        Guid idempotencyKey,
        long? movedByUserId)
        => new(
            productId,
            MovementType.Outbound,
            quantity,
            balanceAfter + quantity,
            balanceAfter,
            invoiceId,
            idempotencyKey,
            movedByUserId);

    public static StockMovement Inbound(Guid productId, int quantity, long? movedByUserId)
        => new(productId, MovementType.Inbound, quantity, 0, quantity, null, null, movedByUserId);
}
