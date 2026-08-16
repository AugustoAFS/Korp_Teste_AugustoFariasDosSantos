using Gateway.Config;
using Gateway.Data;
using Gateway.DependencyInjection;
using Gateway.Middleware;
using Microsoft.AspNetCore.HttpOverrides;

namespace Gateway
{
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
                .AddDependencies(builder.Configuration)
                .AddPersistentDataProtection()
                .AddCookieAuthentication(builder.Environment)
                .AddFrontCors(builder.Configuration)
                .AddRateLimiting()
                .AddDatabaseHealthCheck()
                .AddVersioning()
                .AddValidationContract()
                .AddDownstreamProxy(builder.Configuration)
                .AddDownstreamResilience(builder.Configuration)
                .AddDocumentation();

            builder.Services.AddControllers();

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
            app.UseCors(CorsConfig.Policy);
            app.UseAuthentication();
            app.UseRateLimiter();
            app.UseMiddleware<AntiforgeryMiddleware>();
            app.UseAuthorization();

            app.MapControllers();
            app.MapDownstreamProxy();
            app.MapDatabaseHealthCheck();
            app.MapDocumentation();

            await app.RunAsync();
        }
    }
}
