using Gateway.Exceptions;

namespace Gateway.Dtos;

public class Result
{
    protected Result(int status, Error? error)
    {
        Status = status;
        Error = error;
    }

    public int Status { get; }
    public Error? Error { get; }
    public bool Success => Error is null;

    public static Result NoContent() => new(StatusCodes.Status204NoContent, null);

    public static implicit operator Result(Error error) => new(error.Status, error);
}
