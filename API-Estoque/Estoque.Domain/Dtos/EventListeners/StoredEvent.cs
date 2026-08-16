namespace Estoque.Domain.Dtos.EventListeners;

public sealed record StoredEvent
{
    public string Type { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
}
