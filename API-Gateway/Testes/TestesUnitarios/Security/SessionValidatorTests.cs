using System.Globalization;
using System.Security.Claims;
using Gateway.Models;
using Gateway.Repositories.Interfaces;
using Gateway.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Gateway.TestesUnitarios.Security;

public sealed class SessionValidatorTests
{
    private readonly IUserRepository _usuarios = Substitute.For<IUserRepository>();
    private readonly IAuthenticationService _autenticacao = Substitute.For<IAuthenticationService>();

    private CookieValidatePrincipalContext Contexto(ClaimsPrincipal principal)
    {
        var servicos = new ServiceCollection();
        servicos.AddSingleton(_usuarios);
        servicos.AddSingleton(_autenticacao);

        var http = new DefaultHttpContext { RequestServices = servicos.BuildServiceProvider() };

        return new CookieValidatePrincipalContext(
            http,
            new AuthenticationScheme(
                CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    private static ClaimsPrincipal Sessao(
        string? id = "7", DateTimeOffset? emitidaEm = null, params string[] perfis)
    {
        var identidade = new ClaimsIdentity("cookie", ClaimTypes.Name, ClaimTypes.Role);

        if (id is not null)
            identidade.AddClaim(new Claim(ClaimTypes.NameIdentifier, id));

        identidade.AddClaim(new Claim(
            SessionValidator.IssuedAtClaim,
            (emitidaEm ?? DateTimeOffset.UtcNow).ToString("O", CultureInfo.InvariantCulture)));

        foreach (var perfil in perfis)
            identidade.AddClaim(new Claim(ClaimTypes.Role, perfil));

        return new ClaimsPrincipal(identidade);
    }

    private static User Usuario(params string[] perfis)
    {
        var usuario = new User("Augusto", "augusto@korp.com.br", "$argon2id$hash");

        foreach (var (nome, indice) in perfis.Select((nome, indice) => (nome, indice)))
        {
            usuario.AssignRole(indice + 1);
            typeof(UserRole)
                .GetProperty(nameof(UserRole.Role))!
                .SetValue(usuario.Roles.Last(), new Role(indice + 1, nome, null));
        }

        return usuario;
    }

    private void Existe(User usuario) => _usuarios.ById(7, Arg.Any<CancellationToken>()).Returns(usuario);

    [Fact]
    public async Task Sessao_sem_identificador_e_rejeitada()
    {
        var contexto = Contexto(Sessao(id: null));

        await SessionValidator.Validate(contexto);

        contexto.Principal.ShouldBeNull();
    }

    [Fact]
    public async Task Sessao_de_usuario_inexistente_e_rejeitada()
    {
        _usuarios.ById(7, Arg.Any<CancellationToken>()).Returns((User?)null);
        var contexto = Contexto(Sessao());

        await SessionValidator.Validate(contexto);

        contexto.Principal.ShouldBeNull();
    }

    [Fact]
    public async Task Sessao_continua_valida_quando_nada_mudou()
    {
        var usuario = Usuario("Gerente");
        Existe(usuario);
        var contexto = Contexto(Sessao(emitidaEm: DateTimeOffset.UtcNow.AddMinutes(5), perfis: "Gerente"));

        await SessionValidator.Validate(contexto);

        contexto.Principal.ShouldNotBeNull();
    }

    [Fact]
    public async Task Troca_de_senha_derruba_a_sessao_emitida_antes()
    {
        Existe(Usuario("Gerente"));
        var contexto = Contexto(Sessao(emitidaEm: DateTimeOffset.UtcNow.AddHours(-1), perfis: "Gerente"));

        await SessionValidator.Validate(contexto);

        contexto.Principal.ShouldBeNull();
    }

    [Fact]
    public async Task Perfil_removido_derruba_a_sessao()
    {
        Existe(Usuario("Funcionario"));
        var contexto = Contexto(Sessao(
            emitidaEm: DateTimeOffset.UtcNow.AddMinutes(5), perfis: ["Funcionario", "Gerente"]));

        await SessionValidator.Validate(contexto);

        contexto.Principal.ShouldBeNull();
    }

    [Fact]
    public async Task Perfil_acrescentado_derruba_a_sessao()
    {
        Existe(Usuario("Funcionario", "Gerente"));
        var contexto = Contexto(Sessao(emitidaEm: DateTimeOffset.UtcNow.AddMinutes(5), perfis: "Funcionario"));

        await SessionValidator.Validate(contexto);

        contexto.Principal.ShouldBeNull();
    }

    [Fact]
    public async Task Sessao_rejeitada_tambem_encerra_o_cookie()
    {
        _usuarios.ById(7, Arg.Any<CancellationToken>()).Returns((User?)null);
        var contexto = Contexto(Sessao());

        await SessionValidator.Validate(contexto);

        await _autenticacao.Received(1).SignOutAsync(
            Arg.Any<HttpContext>(),
            CookieAuthenticationDefaults.AuthenticationScheme,
            Arg.Any<AuthenticationProperties>());
    }
}
