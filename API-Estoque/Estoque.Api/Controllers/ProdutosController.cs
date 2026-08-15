using Asp.Versioning;
using Estoque.Api.Configurations;
using Estoque.ApplicationService.Interfaces;
using Estoque.Domain.Dtos.Request;
using Estoque.Domain.Dtos.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
public sealed class ProdutosController(IProductService produtoService) : BaseController
{
    #region GET's

    [HttpGet]
    [Authorize]
    [ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] ProductFilterRequest filtro, CancellationToken ct)
        => Respond(await produtoService.GetProducts(filtro, ct));

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken ct)
        => Respond(await produtoService.GetProductById(id, ct));

    #endregion

    #region POST's

    [HttpPost]
    [Authorize(Policy = AuthConfig.PoliticaDeEscrita)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken ct)
        => Respond(await produtoService.CreateProduct(request, ct));

    #endregion

    #region PUT's

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthConfig.PoliticaDeEscrita)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProduct(
        Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
        => Respond(await produtoService.UpdateProduct(id, request, ct));

    #endregion

    #region DELETE's

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthConfig.PoliticaDeEscrita)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct)
        => Respond(await produtoService.DeleteProduct(id, ct));

    #endregion
}
