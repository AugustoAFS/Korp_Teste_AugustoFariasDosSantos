using Estoque.Api.Configurations;
using Estoque.Api.Data;
using Estoque.Api.Middlewares;
using Estoque.ApplicationService.DependencyInjection;
using Estoque.EventListeners;
using Estoque.InfraStructure.DependencyInjection;
using Microsoft.AspNetCore.HttpOverrides;

namespace Estoque.Api;

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

        await app.PrepareDatabase();

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
