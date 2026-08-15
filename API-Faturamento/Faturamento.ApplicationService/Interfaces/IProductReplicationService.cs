namespace Faturamento.ApplicationService.Interfaces;

public interface IProductReplicationService
{
    Task Replicate(
        Guid messageId,
        string messageType,
        Guid productId,
        string code,
        string description,
        bool active,
        DateTimeOffset occurredAt,
        CancellationToken ct);
}
