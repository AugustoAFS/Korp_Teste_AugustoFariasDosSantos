using Asp.Versioning;
using Gateway.Config;
using Gateway.Dtos.Request;
using Gateway.Dtos.Response;
using Gateway.Models.Enums;
using Gateway.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gateway.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
public sealed class UsersController(IUserService userService) : BaseController
{
    #region GET's

    [HttpGet("{id:long}")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(long id, CancellationToken ct)
        => Respond(await userService.ById(id, ct));

    #endregion

    #region POST's

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitConfig.CredentialsPolicy)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
        => Respond(await userService.Create(request, ct));

    #endregion

    #region PUT's

    [HttpPut("{id:long}/roles")]
    [Authorize(Roles = nameof(DefaultRole.Administrador))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateUserRoles(long id, [FromBody] AssignRolesRequest request, CancellationToken ct)
        => Respond(await userService.ReplaceRoles(id, request, ct));

    #endregion
}
