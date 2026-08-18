namespace Faturamento.Domain.Dtos.Ai;

public sealed record InterpretedItem
{
    public required Guid ProductId { get; init; }
    public required string ProductCode { get; init; }
    public required string ProductDescription { get; init; }
    public required int Quantity { get; init; }
    public required bool AlreadyInInvoice { get; init; }
}
