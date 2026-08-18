namespace Faturamento.Domain.Dtos.Response;

public sealed record InvoicePdf
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
}
