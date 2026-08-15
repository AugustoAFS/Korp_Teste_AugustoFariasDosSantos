using System.Net;
using Faturamento.Domain.Exceptions;

namespace Faturamento.Domain.Dtos;

public sealed class Result<T> : Result
{
    private Result(HttpStatusCode status, T? value, Error? error) : base(status, error) => Value = value;

    public T? Value { get; }

    public static Result<T> Ok(T value) => new(HttpStatusCode.OK, value, null);

    public static Result<T> Created(T value) => new(HttpStatusCode.Created, value, null);

    public static Result<T> Accepted(T value) => new(HttpStatusCode.Accepted, value, null);

    public static implicit operator Result<T>(T value) => Ok(value);

    public static implicit operator Result<T>(Error error) => new(error.Status, default, error);
}
