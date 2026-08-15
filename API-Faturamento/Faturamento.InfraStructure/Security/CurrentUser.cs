using System.Globalization;
using Faturamento.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Faturamento.InfraStructure.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public const string IdClaim = "sub";
    public const string NameClaim = "name";

    private static readonly string[] PerfisComVisaoTotal = ["Administrador", "Gerente"];

    public long? Id
        => long.TryParse(
            Value(IdClaim),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var id)
            ? id
            : null;

    public string Name => Value(NameClaim) ?? string.Empty;

    public bool SeesEveryInvoice
        => accessor.HttpContext?.User is { } usuario && Array.Exists(PerfisComVisaoTotal, usuario.IsInRole);

    private string? Value(string claim) => accessor.HttpContext?.User.FindFirst(claim)?.Value;
}
