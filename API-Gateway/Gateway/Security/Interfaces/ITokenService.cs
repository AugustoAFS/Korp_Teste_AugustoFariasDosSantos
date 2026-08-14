using System.Security.Claims;

namespace Gateway.Security.Interfaces;

public interface ITokenService
{
    string Issue(ClaimsPrincipal principal);
}
