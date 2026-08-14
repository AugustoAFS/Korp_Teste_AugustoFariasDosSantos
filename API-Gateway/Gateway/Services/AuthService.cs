using System.Globalization;
using System.Security.Claims;
using Gateway.Dtos;
using Gateway.Dtos.Request;
using Gateway.Dtos.Response;
using Gateway.Exceptions;
using Gateway.Models;
using Gateway.Repositories.Interfaces;
using Gateway.Security;
using Gateway.Security.Interfaces;
using Gateway.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Gateway.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IArgon2idHasher hasher,
    ITokenService tokenService,
    IHttpContextAccessor accessor,
    ILogger<AuthService> logger) : IAuthService
{
    private const int MaxAttempts = 5;
    private const int LockoutMinutes = 15;

    public async Task<Result> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await userRepository.ByEmail(request.Email, ct);

        if (user?.PasswordHash is null)
        {
            hasher.DummyVerify(request.Password);
            logger.LogWarning("Login recusado: e-mail sem credencial local");
            return Errors.InvalidCredentials;
        }

        if (!hasher.Verify(request.Password, user.PasswordHash))
        {
            if (!user.Locked)
            {
                user.RegisterInvalidAccess(MaxAttempts, TimeSpan.FromMinutes(LockoutMinutes));
                await userRepository.SaveChanges(ct);
            }

            logger.LogWarning("Login recusado: senha inválida para o usuário {UserId}", user.Id);
            return Errors.InvalidCredentials;
        }

        if (user.Locked)
        {
            logger.LogWarning("Login recusado: usuário {UserId} bloqueado até {LockedUntil}", user.Id, user.LockedUntil);
            return Errors.UserLocked;
        }

        if (!user.Active)
        {
            logger.LogWarning("Login recusado: usuário {UserId} inativo", user.Id);
            return Errors.UserInactive;
        }

        user.RegisterValidAccess();
        await userRepository.SaveChanges(ct);

        await accessor.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            PrincipalOf(user),
            new AuthenticationProperties { IsPersistent = true });

        logger.LogInformation("Usuário {UserId} autenticado", user.Id);
        return Result.NoContent();
    }

    public async Task<Result> Logout()
    {
        await accessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Result.NoContent();
    }

    public Result<SessionResponse> Session()
    {
        var principal = accessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
            return Errors.InvalidSession;

        return new SessionResponse(
            principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            principal.FindAll(ClaimTypes.Role).Select(role => role.Value).ToArray());
    }

    public Result<TokenResponse> Token()
    {
        var principal = accessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
            return Errors.InvalidSession;

        return new TokenResponse(tokenService.Issue(principal), TokenService.SecondsToLive);
    }

    private static ClaimsPrincipal PrincipalOf(User user)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        identity.AddClaim(new Claim(SessionValidator.IssuedAtClaim, DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)));

        foreach (var link in user.Roles)
            identity.AddClaim(new Claim(ClaimTypes.Role, link.Role.Name));

        return new ClaimsPrincipal(identity);
    }
}
