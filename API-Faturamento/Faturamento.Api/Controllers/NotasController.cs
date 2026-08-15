using Asp.Versioning;
using Faturamento.ApplicationService.Interfaces;
using Faturamento.Domain.Dtos.Request;
using Faturamento.Domain.Dtos.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faturamento.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
public sealed class NotasController(
    IInvoiceService notaService,
    IInvoicePrintService impressaoService) : BaseController
{
    #region GET's

    [HttpGet]
    [Authorize]
    [ProducesResponseType<PagedResult<InvoiceResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices([FromQuery] InvoiceFilterRequest filtro, CancellationToken ct)
        => Respond(await notaService.GetInvoices(filtro, ct));

    [HttpGet("{id:long}")]
    [Authorize]
    [ProducesResponseType<InvoiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoiceById(long id, CancellationToken ct)
        => Respond(await notaService.GetInvoiceById(id, ct));

    #endregion

    #region POST's

    [HttpPost]
    [Authorize]
    [ProducesResponseType<InvoiceResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInvoice(CancellationToken ct)
        => Respond(await notaService.CreateInvoice(ct));

    [HttpPost("{id:long}/itens")]
    [Authorize]
    [ProducesResponseType<InvoiceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddInvoiceItem(
        long id, [FromBody] AddInvoiceItemRequest request, CancellationToken ct)
        => Respond(await notaService.AddInvoiceItem(id, request, ct));

    [HttpPost("{id:long}/impressao")]
    [Authorize]
    [ProducesResponseType<InvoiceResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PrintInvoice(long id, CancellationToken ct)
        => Respond(await impressaoService.PrintInvoice(id, ct));

    #endregion

    #region PUT's

    [HttpPut("{id:long}/itens/{itemId:long}")]
    [Authorize]
    [ProducesResponseType<InvoiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateInvoiceItem(
        long id, long itemId, [FromBody] UpdateInvoiceItemRequest request, CancellationToken ct)
        => Respond(await notaService.UpdateInvoiceItem(id, itemId, request, ct));

    #endregion

    #region DELETE's

    [HttpDelete("{id:long}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteInvoice(long id, CancellationToken ct)
        => Respond(await notaService.DeleteInvoice(id, ct));

    [HttpDelete("{id:long}/itens/{itemId:long}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteInvoiceItem(long id, long itemId, CancellationToken ct)
        => Respond(await notaService.DeleteInvoiceItem(id, itemId, ct));

    #endregion
}
