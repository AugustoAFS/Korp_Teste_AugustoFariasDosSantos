using Estoque.Domain.Dtos;
using Estoque.Domain.Dtos.Request;
using Estoque.Domain.Dtos.Response;

namespace Estoque.ApplicationService.Interfaces;

public interface IProductService
{
    Task<Result<PagedResult<ProductResponse>>> GetProducts(ProductFilterRequest filtro, CancellationToken ct);

    Task<Result<ProductResponse>> GetProductById(Guid id, CancellationToken ct);

    Task<Result<ProductResponse>> CreateProduct(CreateProductRequest request, CancellationToken ct);

    Task<Result<ProductResponse>> UpdateProduct(Guid id, UpdateProductRequest request, CancellationToken ct);

    Task<Result> DeleteProduct(Guid id, CancellationToken ct);
}
