using Estoque.Domain.Exceptions;

namespace Estoque.Domain.Dtos.EventListeners;

public sealed record DebitResult
{
    public bool Success { get; init; }
    public IReadOnlyList<UpdatedBalance> Items { get; init; } = [];
    public Guid? RejectedProductId { get; init; }
    public string? Reason { get; init; }

    public static DebitResult Ok(IReadOnlyList<UpdatedBalance> items)
        => new() { Success = true, Items = items };

    public static DebitResult Rejected(Guid productId, Error error)
        => new() { Success = false, RejectedProductId = productId, Reason = error.Detail };
}
