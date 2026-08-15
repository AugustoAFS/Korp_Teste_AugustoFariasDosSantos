using Asp.Versioning;
using Gateway.Config;
using Gateway.Dtos.Request;
using Gateway.Dtos.Response;
using Gateway.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gateway.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class AuthController(IAuthService authService) : BaseController
{
    #region GET's

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    public IActionResult GetSession()
        => Respond(authService.Session());

    [HttpGet("token")]
    [Authorize]
    [EnableRateLimiting(RateLimitConfig.CredentialsPolicy)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    public IActionResult GetToken()
        => Respond(authService.Token());

    #endregion

    #region POST's

    [HttpPost("login")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting(RateLimitConfig.CredentialsPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Respond(await authService.Login(request, ct));

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
        => Respond(await authService.Logout());

    #endregion
}
