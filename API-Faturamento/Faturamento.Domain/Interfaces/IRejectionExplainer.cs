namespace Faturamento.Domain.Interfaces;

public interface IRejectionExplainer
{
    bool Enabled { get; }

    Task<string?> Explain(
        string technicalReason, IReadOnlyList<string> invoiceItems, CancellationToken ct);
}
