namespace Faturamento.Domain.Entities;

public sealed class ProcessedMessage
{
    private ProcessedMessage() { }

    public ProcessedMessage(Guid messageId, string type)
    {
        MessageId = messageId;
        Type = type;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public Guid MessageId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; private set; }
}
