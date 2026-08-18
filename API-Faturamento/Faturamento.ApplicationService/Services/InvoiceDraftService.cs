using Faturamento.ApplicationService.Interfaces;
using Faturamento.Domain.Dtos;
using Faturamento.Domain.Dtos.Ai;
using Faturamento.Domain.Dtos.Request;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;
using Faturamento.Domain.Exceptions;
using Faturamento.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Faturamento.ApplicationService.Services;

public sealed class InvoiceDraftService(
    IInvoiceRepository invoices,
    IReplicatedProductRepository products,
    IInvoiceItemInterpreter interpreter,
    ICurrentUser currentUser,
    ILogger<InvoiceDraftService> logger) : IInvoiceDraftService
{
    private const int CatalogLimit = 200;

    public async Task<Result<InterpretationResult>> InterpretItems(
        long invoiceId, InterpretItemsRequest request, CancellationToken ct)
    {
        if (!interpreter.Enabled)
        {
            logger.LogInformation("Interpretação recusada: assistente desligado nesta instalação");
            return Errors.AiDisabled;
        }

        var invoice = await invoices.GetById(invoiceId, ct);

        if (invoice is null || (!currentUser.SeesEveryInvoice && invoice.IssuedByUserId != currentUser.Id))
        {
            logger.LogWarning("Interpretação recusada: nota {Nota} inexistente ou fora do escopo", invoiceId);
            return Errors.InvoiceNotFound;
        }

        if (invoice.Status == InvoiceStatus.Closed) return Errors.InvoiceAlreadyClosed;
        if (invoice.Printing) return Errors.InvoiceAlreadyPrinting;

        var catalog = await products.ActiveCatalog(CatalogLimit, ct);

        if (catalog.Count == 0) return Vazio();

        IReadOnlyList<ParsedItem> parsed;

        try
        {
            parsed = await interpreter.Interpret(
                request.Phrase,
                [.. catalog.Select(produto => new CatalogEntry
                {
                    Code = produto.Code,
                    Description = produto.Description
                })],
                ct);
        }
        catch (Exception excecao)
        {
            logger.LogWarning(excecao, "Assistente indisponível ao interpretar a nota {Nota}", invoiceId);
            return Errors.AiUnavailable;
        }

        return Resolver(parsed, catalog, invoice);
    }

    private static InterpretationResult Vazio()
        => new() { Items = [], Unresolved = [] };

    private static InterpretationResult Resolver(
        IReadOnlyList<ParsedItem> parsed, IReadOnlyList<ReplicatedProduct> catalog, Invoice invoice)
    {
        var porCodigo = catalog.ToDictionary(
            produto => produto.Code, StringComparer.OrdinalIgnoreCase);

        var itens = new List<InterpretedItem>();
        var naoResolvidos = new List<string>();

        foreach (var item in parsed)
        {
            if (!porCodigo.TryGetValue(item.Code, out var produto))
            {
                naoResolvidos.Add(item.Code);
                continue;
            }

            if (itens.Exists(incluido => incluido.ProductId == produto.ProductId)) continue;

            itens.Add(new InterpretedItem
            {
                ProductId = produto.ProductId,
                ProductCode = produto.Code,
                ProductDescription = produto.Description,
                Quantity = item.Quantity,
                AlreadyInInvoice = invoice.HasProduct(produto.ProductId)
            });
        }

        return new InterpretationResult { Items = itens, Unresolved = naoResolvidos };
    }
}
