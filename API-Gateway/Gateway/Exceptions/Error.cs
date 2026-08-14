namespace Gateway.Exceptions;

public sealed record Error(string Code, string Title, int Status, string Detail)
{
    public Error With(string detail) => this with { Detail = detail };
}
