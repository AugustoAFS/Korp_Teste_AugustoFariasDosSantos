using Gateway.Exceptions;

namespace Gateway.Dtos;

public sealed class Result<T> : Result
{
    private Result(int status, T? value, Error? error) : base(status, error) => Value = value;

    public T? Value { get; }

    public static Result<T> Ok(T value) => new(StatusCodes.Status200OK, value, null);

    public static Result<T> Created(T value) => new(StatusCodes.Status201Created, value, null);

    public static implicit operator Result<T>(T value) => Ok(value);

    public static implicit operator Result<T>(Error error) => new(error.Status, default, error);
}
