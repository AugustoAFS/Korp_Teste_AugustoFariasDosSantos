using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;

namespace Faturamento.Domain.Dtos.Response;

public sealed record InvoiceResponse
{
    public InvoiceResponse(Invoice invoice)
    {
        Id = invoice.Id;
        Number = invoice.Number;
        Status = invoice.Status;
        IssuedByUserName = invoice.IssuedByUserName;
        CreatedAt = invoice.CreatedAt;
        ClosedAt = invoice.ClosedAt;
        ProcessingId = invoice.ProcessingId;
        Printing = invoice.Printing;
        Editable = invoice.Editable;
        LastError = invoice.LastError;
        Items = [.. invoice.Items.Select(item => new InvoiceItemResponse(item))];
    }

    public long Id { get; init; }
    public long Number { get; init; }
    public InvoiceStatus Status { get; init; }
    public string IssuedByUserName { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public Guid? ProcessingId { get; init; }
    public bool Printing { get; init; }
    public bool Editable { get; init; }
    public string? LastError { get; init; }
    public IReadOnlyList<InvoiceItemResponse> Items { get; init; } = [];
}
