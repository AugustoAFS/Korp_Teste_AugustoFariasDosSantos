using Estoque.ApplicationService.Interfaces;
using Estoque.Domain.Dtos;
using Estoque.Domain.Dtos.Request;
using Estoque.Domain.Dtos.Response;
using Estoque.Domain.Entities;
using Estoque.Domain.Exceptions;
using Estoque.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Estoque.ApplicationService.Services;

public sealed class ProductService(
    IProductRepository products,
    IStockMovementRepository movements,
    IEstoqueEventPublisher publisher,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<ProductService> logger) : IProductService
{
    public async Task<Result<PagedResult<ProductResponse>>> GetProducts(ProductFilterRequest filter, CancellationToken ct)
    {
        var (items, total) = await products.GetPaged(filter, ct);

        return new PagedResult<ProductResponse>
        {
            Items = [.. items.Select(product => new ProductResponse(product))],
            Page = filter.Page,
            Size = filter.Size,
            Total = total
        };
    }

    public async Task<Result<ProductResponse>> GetProductById(Guid id, CancellationToken ct)
    {
        var product = await products.GetById(id, ct);

        if (product is null)
        {
            logger.LogWarning("Consulta recusada: produto {Produto} inexistente", id);
            return Errors.ProductNotFound;
        }

        return new ProductResponse(product);
    }

    public async Task<Result<ProductResponse>> CreateProduct(CreateProductRequest request, CancellationToken ct)
    {
        if (await products.CodeInUse(request.Code, null, ct))
        {
            logger.LogWarning("Criação recusada: código {Codigo} já cadastrado", request.Code);
            return Errors.CodeInUse;
        }

        var product = new Product(request.Code, request.Description, request.Balance);

        await unitOfWork.Begin(ct);

        try
        {
            await products.Add(product, ct);
            await movements.AddRange([StockMovement.Inbound(product.Id, request.Balance, currentUser.Id)], ct);
            await publisher.PublishProductCreated(product.Id, product.Code, product.Description, product.Active, ct);

            if (!await unitOfWork.SaveWithoutConflict(ct))
            {
                await unitOfWork.Rollback(ct);
                logger.LogWarning("Criação recusada: código {Codigo} cadastrado em requisição concorrente", request.Code);
                return Errors.CodeInUse;
            }

            await unitOfWork.Commit(ct);
        }
        catch
        {
            await unitOfWork.Rollback(ct);
            throw;
        }

        logger.LogInformation("Produto {Produto} criado com código {Codigo}", product.Id, product.Code);
        return Result<ProductResponse>.Created(new ProductResponse(product));
    }

    public async Task<Result<ProductResponse>> UpdateProduct(Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        var product = await products.GetById(id, ct);

        if (product is null)
        {
            logger.LogWarning("Atualização recusada: produto {Produto} inexistente", id);
            return Errors.ProductNotFound;
        }

        if (await products.CodeInUse(request.Code, id, ct))
        {
            logger.LogWarning("Atualização recusada: código {Codigo} já cadastrado", request.Code);
            return Errors.CodeInUse;
        }

        product.Update(request.Code, request.Description, request.Active);

        await unitOfWork.Begin(ct);

        try
        {
            await publisher.PublishProductUpdated(product.Id, product.Code, product.Description, product.Active, ct);

            if (!await unitOfWork.SaveWithoutConflict(ct))
            {
                await unitOfWork.Rollback(ct);
                logger.LogWarning("Atualização recusada: código {Codigo} cadastrado em requisição concorrente", request.Code);
                return Errors.CodeInUse;
            }

            await unitOfWork.Commit(ct);
        }
        catch
        {
            await unitOfWork.Rollback(ct);
            throw;
        }

        logger.LogInformation("Produto {Produto} atualizado", product.Id);
        return new ProductResponse(product);
    }

    public async Task<Result> DeleteProduct(Guid id, CancellationToken ct)
    {
        var product = await products.GetById(id, ct);

        if (product is null)
        {
            logger.LogWarning("Exclusão recusada: produto {Produto} inexistente", id);
            return Errors.ProductNotFound;
        }

        if (product.Balance > 0)
        {
            logger.LogWarning("Exclusão recusada: produto {Produto} com saldo {Saldo}", id, product.Balance);
            return Errors.ProductWithBalance;
        }

        product.Delete();

        await unitOfWork.Begin(ct);

        try
        {
            await publisher.PublishProductUpdated(product.Id, product.Code, product.Description, product.Active, ct);

            await unitOfWork.SaveWithoutConflict(ct);
            await unitOfWork.Commit(ct);
        }
        catch
        {
            await unitOfWork.Rollback(ct);
            throw;
        }

        logger.LogInformation("Produto {Produto} excluído", product.Id);
        return Result.NoContent();
    }
}
