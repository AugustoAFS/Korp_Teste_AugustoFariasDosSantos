using System.Globalization;
using System.Security.Claims;
using Gateway.Models;
using Gateway.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Gateway.Security;

public static class SessionValidator
{
    public const string IssuedAtClaim = "session_issued_at";

    public static async Task Validate(CookieValidatePrincipalContext context)
    {
        if (!long.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            await Reject(context);
            return;
        }

        var repository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var user = await repository.ById(userId, context.HttpContext.RequestAborted);

        if (user is null || !user.Active || PasswordChanged(context, user) || RolesChanged(context, user))
            await Reject(context);
    }

    private static bool PasswordChanged(CookieValidatePrincipalContext context, User user)
    {
        if (user.PasswordChangedAt is null) return false;

        var issuedAt = context.Principal?.FindFirstValue(IssuedAtClaim);

        return !DateTimeOffset.TryParse(issuedAt, CultureInfo.InvariantCulture, out var moment)
               || user.PasswordChangedAt > moment;
    }

    private static bool RolesChanged(CookieValidatePrincipalContext context, User user)
    {
        var current = user.Roles.Select(link => link.Role.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var granted = context.Principal!.FindAll(ClaimTypes.Role).Select(claim => claim.Value);

        return !current.SetEquals(granted);
    }

    private static async Task Reject(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();

        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
