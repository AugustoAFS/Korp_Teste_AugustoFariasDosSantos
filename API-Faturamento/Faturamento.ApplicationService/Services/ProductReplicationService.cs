using Faturamento.ApplicationService.Interfaces;
using Faturamento.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Faturamento.ApplicationService.Services;

public sealed class ProductReplicationService(
    IReplicatedProductRepository products,
    IProcessedMessageRepository messages,
    IUnitOfWork unitOfWork,
    ILogger<ProductReplicationService> logger) : IProductReplicationService
{
    public async Task Replicate(
        Guid messageId,
        string messageType,
        Guid productId,
        string code,
        string description,
        bool active,
        DateTimeOffset occurredAt,
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

            await products.Upsert(productId, code, description, active, occurredAt, ct);

            await unitOfWork.SaveWithoutConflict(ct);
            await unitOfWork.Commit(ct);

            logger.LogInformation("Produto {Produto} replicado com código {Codigo}", productId, code);
        }
        catch
        {
            await unitOfWork.Rollback(ct);
            throw;
        }
    }
}
