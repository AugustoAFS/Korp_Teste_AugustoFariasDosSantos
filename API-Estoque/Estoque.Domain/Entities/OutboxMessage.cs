namespace Estoque.Domain.Entities;

public sealed class OutboxMessage
{
    private OutboxMessage() { }

    public OutboxMessage(string type, string payload)
    {
        Id = Guid.CreateVersion7();
        Type = type;
        Payload = payload;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
}
