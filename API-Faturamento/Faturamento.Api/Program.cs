using Faturamento.Api.Configurations;
using Faturamento.Api.Middlewares;
using Faturamento.ApplicationService.DependencyInjection;
using Faturamento.EventListeners;
using Faturamento.InfraStructure.Data;
using Faturamento.InfraStructure.DependencyInjection;
using Microsoft.AspNetCore.HttpOverrides;

namespace Faturamento.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });

        builder.Services
            .AddInfraStructure(builder.Configuration)
            .AddApplicationService()
            .AddEventListeners(builder.Configuration)
            .AddJwtAuthentication(builder.Configuration)
            .AddFrontCors(builder.Configuration)
            .AddRateLimiting()
            .AddDatabaseHealthCheck()
            .AddVersioning()
            .AddValidationContract()
            .AddDocumentation();

        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();

        var app = builder.Build();

        await app.Services.PrepareDatabase();

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseMiddleware<ExceptionMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseRouting();
        app.UseCors(CorsConfig.Politica);
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();

        app.MapControllers();
        app.MapDatabaseHealthCheck();
        app.MapDocumentation();

        await app.RunAsync();
    }
}
