using System.Globalization;
using Estoque.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Estoque.InfraStructure.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public const string Claim = "sub";

    public long? Id
        => long.TryParse(
            accessor.HttpContext?.User.FindFirst(Claim)?.Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var id)
            ? id
            : null;
}
