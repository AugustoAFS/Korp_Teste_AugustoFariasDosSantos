using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Faturamento.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Faturamento.TestesIntegracao.Suporte;

public sealed class FaturamentoApiFactory(PostgresFixture banco, RabbitMqFixture broker)
    : WebApplicationFactory<Program>
{
    private const string Emissor = "emissor-gateway";
    private const string Audiencia = "emissor-servicos";
    private const string Chave = "LjYrUfDqiP3ZYVLilk7D_V2BQs6s6larC-nhKVO0UNJNpJ6418pe_U3tqG1-4wrQ";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.UseSetting("ConnectionStrings:FaturamentoDb", banco.ConnectionString);
        builder.UseSetting("ConnectionStrings:RabbitMq", broker.ConnectionString);
        builder.UseSetting("Security:JwtKey", Chave);
        builder.UseSetting("Cors:Origins:0", "http://localhost:4200");
    }

    public HttpClient ClienteComPerfil(long usuarioId, string nome, params string[] perfis)
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new("Bearer", Token(usuarioId, nome, perfis));
        return cliente;
    }

    public HttpClient ClienteAnonimo() => CreateClient();

    private static string Token(long usuarioId, string nome, params string[] perfis)
    {
        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Chave)), SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new("name", nome)
        ];

        claims.AddRange(perfis.Select(perfil => new Claim("role", perfil)));

        var token = new JwtSecurityToken(
            issuer: Emissor,
            audience: Audiencia,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
