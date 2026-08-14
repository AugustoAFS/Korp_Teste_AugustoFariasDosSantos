using Gateway.Exceptions;
using Gateway.Middleware;
using Gateway.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace Gateway.Config;

public static class AuthConfig
{
    public const string SessionCookie = "emissor.sessao";

    private const string AntiforgeryCookie = "emissor.antiforgery";
    private const int HoursToLive = 8;

    public static IServiceCollection AddCookieAuthentication(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = SessionCookie;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = SecurePolicy(environment);

                options.ExpireTimeSpan = TimeSpan.FromHours(HoursToLive);
                options.SlidingExpiration = true;

                options.Events.OnValidatePrincipal = SessionValidator.Validate;

                options.Events.OnRedirectToLogin = context =>
                    ProblemResponse.Write(context.HttpContext, Errors.InvalidSession).AsTask();

                options.Events.OnRedirectToAccessDenied = context =>
                    ProblemResponse.Write(context.HttpContext, Errors.Forbidden).AsTask();
            });

        services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

        services.AddAntiforgery(options =>
        {
            options.HeaderName = AntiforgeryMiddleware.TokenHeader;
            options.Cookie.Name = AntiforgeryCookie;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = SecurePolicy(environment);
        });

        return services;
    }

    public static CookieSecurePolicy SecurePolicy(IWebHostEnvironment environment)
        => environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
}
