using System.Net;

namespace Faturamento.Domain.Exceptions;

public sealed record Error
{
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public HttpStatusCode Status { get; init; }
    public string Detail { get; init; } = string.Empty;

    public Error With(string detail) => this with { Detail = detail };
}
