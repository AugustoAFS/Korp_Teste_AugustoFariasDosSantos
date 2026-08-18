using Faturamento.Domain.Enums;

namespace Faturamento.Domain.Entities;

public sealed class Invoice : AuditableEntity
{
    private readonly List<InvoiceItem> _items = [];

    private Invoice() { }

    public Invoice(long number, long issuedByUserId, string issuedByUserName)
    {
        Number = number;
        IssuedByUserId = issuedByUserId;
        IssuedByUserName = issuedByUserName;
        Status = InvoiceStatus.Open;
    }

    public long Id { get; private set; }
    public long Number { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public long IssuedByUserId { get; private set; }
    public string IssuedByUserName { get; private set; } = string.Empty;
    public DateTimeOffset? ClosedAt { get; private set; }
    public Guid? ProcessingId { get; private set; }
    public DateTimeOffset? ProcessingStartedAt { get; private set; }
    public string? LastError { get; private set; }
    public string? RejectionExplanation { get; private set; }
    public IReadOnlyCollection<InvoiceItem> Items => _items;

    public bool Printing => ProcessingId is not null && LastError is null;
    public bool Editable => Status == InvoiceStatus.Open && !Printing;

    public void AddItem(Guid productId, string productCode, string productDescription, int quantity)
        => _items.Add(new InvoiceItem(productId, productCode, productDescription, quantity));

    public bool HasProduct(Guid productId) => _items.Exists(item => item.ProductId == productId);

    public InvoiceItem? ItemById(long itemId) => _items.Find(item => item.Id == itemId);

    public void RemoveItem(InvoiceItem item) => _items.Remove(item);

    public void StartPrinting(Guid processingId)
    {
        ProcessingId = processingId;
        ProcessingStartedAt = DateTimeOffset.UtcNow;
        LastError = null;
        RejectionExplanation = null;
    }

    public void Close()
    {
        Status = InvoiceStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        ProcessingId = null;
        ProcessingStartedAt = null;
        LastError = null;
        RejectionExplanation = null;
    }

    public void Reject(string reason, string? explanation)
    {
        ProcessingId = null;
        ProcessingStartedAt = null;
        LastError = reason;
        RejectionExplanation = explanation;
    }

    public void ExpirePrinting(string reason) => LastError = reason;

    public void Delete() => DeletedAt = DateTimeOffset.UtcNow;
}
