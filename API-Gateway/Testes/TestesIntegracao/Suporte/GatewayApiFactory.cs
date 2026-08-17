using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Gateway.TestesIntegracao.Suporte;

public sealed class GatewayApiFactory(PostgresFixture banco, string? downstream = null)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.UseSetting("ConnectionStrings:GatewayDb", banco.ConnectionString);

        if (downstream is null) return;

        builder.UseSetting("ReverseProxy:Clusters:estoque:Destinations:primary:Address", downstream);
        builder.UseSetting("ReverseProxy:Clusters:faturamento:Destinations:primary:Address", downstream);
    }

    public HttpClient Cliente() => CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = true,
        AllowAutoRedirect = false
    });
}
