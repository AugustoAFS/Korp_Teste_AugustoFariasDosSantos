namespace Estoque.Domain.Dtos.Response;

public sealed record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int Size { get; init; }
    public int Total { get; init; }
    public int TotalPages => Size <= 0 ? 0 : (int)Math.Ceiling(Total / (double)Size);
}
