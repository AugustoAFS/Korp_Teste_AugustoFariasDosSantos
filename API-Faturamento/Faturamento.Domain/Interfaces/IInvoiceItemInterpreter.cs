using Faturamento.Domain.Dtos.Ai;

namespace Faturamento.Domain.Interfaces;

public interface IInvoiceItemInterpreter
{
    bool Enabled { get; }

    Task<IReadOnlyList<ParsedItem>> Interpret(
        string phrase, IReadOnlyList<CatalogEntry> catalog, CancellationToken ct);
}
