using Faturamento.ApplicationService.Interfaces;
using Faturamento.Domain.Dtos;
using Faturamento.Domain.Dtos.Request;
using Faturamento.Domain.Dtos.Response;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;
using Faturamento.Domain.Exceptions;
using Faturamento.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Faturamento.ApplicationService.Services;

public sealed class InvoiceService(
    IInvoiceRepository invoices,
    IReplicatedProductRepository products,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<InvoiceService> logger) : IInvoiceService
{
    public async Task<Result<PagedResult<InvoiceResponse>>> GetInvoices(
        InvoiceFilterRequest filter, CancellationToken ct)
    {
        if (currentUser.Id is null) return Errors.InvalidSession;

        var onlyUserId = currentUser.SeesEveryInvoice ? null : currentUser.Id;

        var (items, total) = await invoices.GetPaged(filter, onlyUserId, ct);

        return new PagedResult<InvoiceResponse>
        {
            Items = [.. items.Select(invoice => new InvoiceResponse(invoice))],
            Page = filter.Page,
            Size = filter.Size,
            Total = total
        };
    }

    public async Task<Result<InvoiceResponse>> GetInvoiceById(long id, CancellationToken ct)
    {
        var invoice = await Visible(id, ct);

        if (invoice is null)
        {
            logger.LogWarning("Consulta recusada: nota {Nota} inexistente ou fora do escopo do usuário", id);
            return Errors.InvoiceNotFound;
        }

        return new InvoiceResponse(invoice);
    }

    public async Task<Result<InvoiceResponse>> CreateInvoice(CancellationToken ct)
    {
        if (currentUser.Id is null) return Errors.InvalidSession;

        await unitOfWork.Begin(ct);

        try
        {
            var number = await invoices.NextNumber(ct);
            var invoice = new Invoice(number, currentUser.Id.Value, currentUser.Name);

            await invoices.Add(invoice, ct);
            await unitOfWork.SaveWithoutConflict(ct);
            await unitOfWork.Commit(ct);

            logger.LogInformation("Nota {Nota} aberta com número {Numero}", invoice.Id, invoice.Number);
            return Result<InvoiceResponse>.Created(new InvoiceResponse(invoice));
        }
        catch
        {
            await unitOfWork.Rollback(ct);
            throw;
        }
    }

    public async Task<Result> DeleteInvoice(long id, CancellationToken ct)
    {
        var invoice = await Visible(id, ct);

        if (invoice is null)
        {
            logger.LogWarning("Exclusão recusada: nota {Nota} inexistente ou fora do escopo do usuário", id);
            return Errors.InvoiceNotFound;
        }

        var blocked = Blocked(invoice);
        if (blocked is not null) return blocked;

        invoice.Delete();

        await unitOfWork.SaveWithoutConflict(ct);

        logger.LogInformation("Nota {Nota} excluída", invoice.Id);
        return Result.NoContent();
    }

    public async Task<Result<InvoiceResponse>> AddInvoiceItem(
        long id, AddInvoiceItemRequest request, CancellationToken ct)
    {
        var invoice = await Visible(id, ct);

        if (invoice is null)
        {
            logger.LogWarning("Inclusão recusada: nota {Nota} inexistente ou fora do escopo do usuário", id);
            return Errors.InvoiceNotFound;
        }

        var blocked = Blocked(invoice);
        if (blocked is not null) return blocked;

        var product = await products.GetById(request.ProductId, ct);

        if (product is null)
        {
            logger.LogWarning("Inclusão recusada: produto {Produto} ainda não replicado", request.ProductId);
            return Errors.ProductNotFound;
        }

        if (!product.Active)
        {
            logger.LogWarning("Inclusão recusada: produto {Produto} inativo", request.ProductId);
            return Errors.ProductInactive;
        }

        if (invoice.HasProduct(request.ProductId))
        {
            logger.LogWarning("Inclusão recusada: produto {Produto} já consta na nota {Nota}", request.ProductId, id);
            return Errors.InvoiceItemDuplicated;
        }

        invoice.AddItem(product.ProductId, product.Code, product.Description, request.Quantity);

        if (!await unitOfWork.SaveWithoutConflict(ct))
        {
            logger.LogWarning("Inclusão recusada: produto {Produto} incluído em requisição concorrente", request.ProductId);
            return Errors.InvoiceItemDuplicated;
        }

        logger.LogInformation("Item do produto {Produto} incluído na nota {Nota}", request.ProductId, invoice.Id);
        return Result<InvoiceResponse>.Created(new InvoiceResponse(invoice));
    }

    public async Task<Result<InvoiceResponse>> UpdateInvoiceItem(
        long id, long itemId, UpdateInvoiceItemRequest request, CancellationToken ct)
    {
        var invoice = await Visible(id, ct);

        if (invoice is null)
        {
            logger.LogWarning("Alteração recusada: nota {Nota} inexistente ou fora do escopo do usuário", id);
            return Errors.InvoiceNotFound;
        }

        var blocked = Blocked(invoice);
        if (blocked is not null) return blocked;

        var item = invoice.ItemById(itemId);

        if (item is null)
        {
            logger.LogWarning("Alteração recusada: item {Item} inexistente na nota {Nota}", itemId, id);
            return Errors.InvoiceItemNotFound;
        }

        item.ChangeQuantity(request.Quantity);

        await unitOfWork.SaveWithoutConflict(ct);

        logger.LogInformation("Item {Item} da nota {Nota} alterado para {Quantidade}", itemId, id, request.Quantity);
        return new InvoiceResponse(invoice);
    }

    public async Task<Result<InvoiceResponse>> DeleteInvoiceItem(long id, long itemId, CancellationToken ct)
    {
        var invoice = await Visible(id, ct);

        if (invoice is null)
        {
            logger.LogWarning("Remoção recusada: nota {Nota} inexistente ou fora do escopo do usuário", id);
            return Errors.InvoiceNotFound;
        }

        var blocked = Blocked(invoice);
        if (blocked is not null) return blocked;

        var item = invoice.ItemById(itemId);

        if (item is null)
        {
            logger.LogWarning("Remoção recusada: item {Item} inexistente na nota {Nota}", itemId, id);
            return Errors.InvoiceItemNotFound;
        }

        invoice.RemoveItem(item);

        await unitOfWork.SaveWithoutConflict(ct);

        logger.LogInformation("Item {Item} removido da nota {Nota}", itemId, id);
        return new InvoiceResponse(invoice);
    }

    private async Task<Invoice?> Visible(long id, CancellationToken ct)
    {
        var invoice = await invoices.GetById(id, ct);

        if (invoice is null) return null;

        if (currentUser.SeesEveryInvoice) return invoice;

        return invoice.IssuedByUserId == currentUser.Id ? invoice : null;
    }

    private static Error? Blocked(Invoice invoice)
    {
        if (invoice.Status == InvoiceStatus.Closed) return Errors.InvoiceAlreadyClosed;

        return invoice.Printing ? Errors.InvoiceAlreadyPrinting : null;
    }
}
