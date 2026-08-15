using System.Net;
using Faturamento.Domain.Exceptions;

namespace Faturamento.Domain.Dtos;

public class Result
{
    protected Result(HttpStatusCode status, Error? error)
    {
        Status = status;
        Error = error;
    }

    public HttpStatusCode Status { get; }
    public Error? Error { get; }
    public bool Success => Error is null;

    public static Result NoContent() => new(HttpStatusCode.NoContent, null);

    public static implicit operator Result(Error error) => new(error.Status, error);
}
