namespace Faturamento.Domain.Dtos.EventListeners;

public sealed record PendingOutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
}
