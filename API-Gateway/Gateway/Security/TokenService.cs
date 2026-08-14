using System.Security.Claims;
using System.Text;
using Gateway.Security.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Security;

public sealed class TokenService(string jwtKey) : ITokenService
{
    public const string Issuer = "emissor-gateway";
    public const string Audience = "emissor-servicos";
    public const int SecondsToLive = 120;

    private static readonly JsonWebTokenHandler Handler = new();

    private readonly SigningCredentials _credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        SecurityAlgorithms.HmacSha256);

    public string Issue(ClaimsPrincipal principal)
    {
        var issuedAt = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString())
        };

        claims.AddRange(principal.FindAll(ClaimTypes.Role).Select(role => new Claim("role", role.Value)));

        return Handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = issuedAt.AddSeconds(SecondsToLive),
            SigningCredentials = _credentials
        });
    }
}
