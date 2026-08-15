using Faturamento.ApplicationService.Interfaces;
using Faturamento.Domain.Dtos;
using Faturamento.Domain.Dtos.EventListeners;
using Faturamento.Domain.Dtos.Response;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;
using Faturamento.Domain.Exceptions;
using Faturamento.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Faturamento.ApplicationService.Services;

public sealed class InvoicePrintService(
    IInvoiceRepository invoices,
    IProcessedMessageRepository messages,
    IFaturamentoEventPublisher publisher,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<InvoicePrintService> logger) : IInvoicePrintService
{
    private const string StockDebited = "EstoqueBaixadoEvent";
    private const string StockRejected = "EstoqueRejeitadoEvent";
    private const string ExpirationReason = "O estoque não respondeu dentro do tempo esperado. Tente imprimir novamente.";

    public async Task<Result<InvoiceResponse>> PrintInvoice(long id, CancellationToken ct)
    {
        var invoice = await invoices.GetById(id, ct);

        if (invoice is null || (!currentUser.SeesEveryInvoice && invoice.IssuedByUserId != currentUser.Id))
        {
            logger.LogWarning("Impressão recusada: nota {Nota} inexistente ou fora do escopo do usuário", id);
            return Errors.InvoiceNotFound;
        }

        if (invoice.Status == InvoiceStatus.Closed)
        {
            logger.LogWarning("Impressão recusada: nota {Nota} já impressa", id);
            return Errors.InvoiceAlreadyClosed;
        }

        if (invoice.Printing)
        {
            logger.LogWarning("Impressão recusada: nota {Nota} já em processamento", id);
            return Errors.InvoiceAlreadyPrinting;
        }

        if (invoice.Items.Count == 0)
        {
            logger.LogWarning("Impressão recusada: nota {Nota} sem itens", id);
            return Errors.InvoiceEmpty;
        }

        var retry = invoice.ProcessingId is not null;
        var processingId = invoice.ProcessingId ?? Guid.NewGuid();

        await unitOfWork.Begin(ct);

        try
        {
            var reserved = retry
                ? await invoices.RestartPrinting(id, processingId, ct)
                : await invoices.StartPrinting(id, processingId, ct);

            if (!reserved)
            {
                await unitOfWork.Rollback(ct);
                logger.LogWarning("Impressão recusada: nota {Nota} reservada em requisição concorrente", id);
                return Errors.InvoiceAlreadyPrinting;
            }

            invoice.StartPrinting(processingId);

            var items = invoice.Items
                .Select(item => new DebitItem { ProductId = item.ProductId, Quantity = item.Quantity })
                .ToList();

            await publisher.PublishDebitStock(id, processingId, currentUser.Id, items, ct);
            await unitOfWork.SaveWithoutConflict(ct);
            await unitOfWork.Commit(ct);
        }
        catch
        {
            await unitOfWork.Rollback(ct);
            throw;
        }

        logger.LogInformation(
            "Impressão da nota {Nota} solicitada · processamento {Processamento} · retentativa {Retentativa}",
            id, processingId, retry);

        return Result<InvoiceResponse>.Accepted(new InvoiceResponse(invoice));
    }

    public async Task CloseInvoice(Guid messageId, long invoiceId, Guid processingId, CancellationToken ct)
    {
        await Apply(messageId, StockDebited, invoiceId, processingId, invoice =>
        {
            invoice.Close();

            logger.LogInformation(
                "Nota {Nota} fechada · processamento {Processamento}", invoiceId, processingId);
        }, ct);
    }

    public async Task RejectInvoice(
        Guid messageId, long invoiceId, Guid processingId, string reason, CancellationToken ct)
    {
        await Apply(messageId, StockRejected, invoiceId, processingId, invoice =>
        {
            invoice.Reject(reason);

            logger.LogWarning(
                "Nota {Nota} rejeitada pelo estoque · processamento {Processamento} · {Motivo}",
                invoiceId, processingId, reason);
        }, ct);
    }

    public async Task<int> ExpirePrintings(TimeSpan timeout, int limit, CancellationToken ct)
    {
        var expired = await invoices.Expired(timeout, limit, ct);

        if (expired.Count == 0) return 0;

        foreach (var invoice in expired)
        {
            invoice.ExpirePrinting(ExpirationReason);

            logger.LogWarning(
                "Impressão da nota {Nota} expirada · processamento {Processamento}",
                invoice.Id, invoice.ProcessingId);
        }

        await unitOfWork.SaveWithoutConflict(ct);

        return expired.Count;
    }

    private async Task Apply(
        Guid messageId,
        string messageType,
        long invoiceId,
        Guid processingId,
        Action<Invoice> transition,
        CancellationToken ct)
    {
        await unitOfWork.Begin(ct);

        try
        {
            if (await messages.AlreadyProcessed(messageId, ct))
            {
                await unitOfWork.Rollback(ct);
                logger.LogInformation("Mensagem {Mensagem} ignorada: já processada", messageId);
                return;
            }

            await messages.Mark(messageId, messageType, ct);

            if (!await unitOfWork.SaveWithoutConflict(ct))
            {
                await unitOfWork.Rollback(ct);
                logger.LogInformation("Mensagem {Mensagem} ignorada: processada em consumo concorrente", messageId);
                return;
            }

            var invoice = await invoices.GetByProcessing(invoiceId, processingId, ct);

            if (invoice is null)
            {
                await unitOfWork.Commit(ct);
                logger.LogWarning(
                    "Mensagem {Mensagem} descartada: nota {Nota} não está no processamento {Processamento}",
                    messageId, invoiceId, processingId);
                return;
            }

            transition(invoice);

            await unitOfWork.SaveWithoutConflict(ct);
            await unitOfWork.Commit(ct);
        }
        catch
        {
            await unitOfWork.Rollback(ct);
            throw;
        }
    }
}
