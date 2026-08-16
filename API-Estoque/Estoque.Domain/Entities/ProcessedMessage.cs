namespace Estoque.Domain.Entities;

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
    public string? OutcomeType { get; private set; }
    public string? OutcomePayload { get; private set; }

    public void RecordOutcome(string outcomeType, string outcomePayload)
    {
        OutcomeType = outcomeType;
        OutcomePayload = outcomePayload;
    }
}
