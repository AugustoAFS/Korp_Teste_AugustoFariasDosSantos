using Estoque.ApplicationService.Interfaces;
using Estoque.Domain.Dtos.EventListeners;
using Estoque.Domain.Entities;
using Estoque.Domain.Exceptions;
using Estoque.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Estoque.ApplicationService.Services;

public sealed class StockDebitService(
    IProductRepository products,
    IStockMovementRepository movements,
    IProcessedMessageRepository messages,
    IEstoqueEventPublisher publisher,
    IUnitOfWork unitOfWork,
    ILogger<StockDebitService> logger) : IStockDebitService
{
    private const string MessageType = "BaixarEstoqueCommand";
    private const string BeforeDebits = "before_debits";

    public async Task<DebitResult> DebitStock(
        long invoiceId,
        Guid processingId,
        long? userId,
        IReadOnlyList<DebitItem> items,
        CancellationToken ct)
    {
        await unitOfWork.Begin(ct);

        try
        {
            if (await messages.AlreadyProcessed(processingId, ct))
                return await SkipDuplicate(processingId, ct);

            await messages.Mark(processingId, MessageType, ct);

            if (!await unitOfWork.SaveWithoutConflict(ct))
                return await SkipDuplicate(processingId, ct);

            await unitOfWork.CreateSavepoint(BeforeDebits, ct);

            var balances = new List<UpdatedBalance>(items.Count);

            foreach (var item in items.OrderBy(i => i.ProductId))
            {
                var newBalance = await products.Debit(item.ProductId, item.Quantity, ct);

                if (newBalance is null)
                    return await RejectDebit(invoiceId, processingId, item, ct);

                balances.Add(new UpdatedBalance { ProductId = item.ProductId, NewBalance = newBalance.Value });
            }

            return await CompleteDebit(invoiceId, processingId, userId, items, balances, ct);
        }
        catch
        {
            await unitOfWork.Rollback(ct);
            throw;
        }
    }

    private async Task<DebitResult> SkipDuplicate(Guid processingId, CancellationToken ct)
    {
        await unitOfWork.Rollback(ct);

        logger.LogInformation("Baixa ignorada: processamento {Processamento} já aplicado", processingId);

        return DebitResult.Ok([]);
    }

    private async Task<DebitResult> RejectDebit(
        long invoiceId, Guid processingId, DebitItem item, CancellationToken ct)
    {
        await unitOfWork.RollbackToSavepoint(BeforeDebits, ct);

        var error = Errors.InsufficientBalance;

        await publisher.PublishStockRejected(invoiceId, processingId, item.ProductId, error.Detail, ct);
        await unitOfWork.SaveWithoutConflict(ct);
        await unitOfWork.Commit(ct);

        logger.LogWarning(
            "Baixa rejeitada: produto {Produto} sem saldo para {Quantidade} unidade(s) na nota {Nota}",
            item.ProductId, item.Quantity, invoiceId);

        return DebitResult.Rejected(item.ProductId, error);
    }

    private async Task<DebitResult> CompleteDebit(
        long invoiceId,
        Guid processingId,
        long? userId,
        IReadOnlyList<DebitItem> items,
        IReadOnlyList<UpdatedBalance> balances,
        CancellationToken ct)
    {
        var byProduct = balances.ToDictionary(balance => balance.ProductId, balance => balance.NewBalance);

        await movements.AddRange(
            [.. items.Select(item => StockMovement.Outbound(
                item.ProductId,
                item.Quantity,
                byProduct[item.ProductId],
                invoiceId,
                processingId,
                userId))],
            ct);

        await publisher.PublishStockDebited(invoiceId, processingId, balances, ct);
        await unitOfWork.SaveWithoutConflict(ct);
        await unitOfWork.Commit(ct);

        logger.LogInformation(
            "Baixa concluída: nota {Nota} · processamento {Processamento} · {Quantidade} itens",
            invoiceId, processingId, items.Count);

        return DebitResult.Ok(balances);
    }
}
