using System.Security.Claims;
using Gateway.Dtos.Request;
using Gateway.Models;
using Gateway.Repositories.Interfaces;
using Gateway.Security.Interfaces;
using Gateway.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Gateway.TestesUnitarios.Services;

public sealed class AuthServiceTests
{
    private readonly IUserRepository _usuarios = Substitute.For<IUserRepository>();
    private readonly IArgon2idHasher _hasher = Substitute.For<IArgon2idHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IHttpContextAccessor _accessor = Substitute.For<IHttpContextAccessor>();
    private readonly IAuthenticationService _autenticacao = Substitute.For<IAuthenticationService>();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var servicos = new ServiceCollection();
        servicos.AddSingleton(_autenticacao);

        var contexto = new DefaultHttpContext { RequestServices = servicos.BuildServiceProvider() };
        _accessor.HttpContext.Returns(contexto);

        _service = new AuthService(
            _usuarios, _hasher, _tokens, _accessor, NullLogger<AuthService>.Instance);
    }

    private static User Usuario(string hash = "$argon2id$hash") => new("Augusto", "augusto@korp.com.br", hash);

    private static LoginRequest Credencial(string senha = "Senha@123")
        => new() { Email = "augusto@korp.com.br", Password = senha };

    private void Autenticar(params string[] perfis)
    {
        var identidade = new ClaimsIdentity("cookie", ClaimTypes.Name, ClaimTypes.Role);
        identidade.AddClaim(new Claim(ClaimTypes.NameIdentifier, "7"));
        identidade.AddClaim(new Claim(ClaimTypes.Name, "Augusto"));
        identidade.AddClaim(new Claim(ClaimTypes.Email, "augusto@korp.com.br"));

        foreach (var perfil in perfis)
            identidade.AddClaim(new Claim(ClaimTypes.Role, perfil));

        _accessor.HttpContext!.User = new ClaimsPrincipal(identidade);
    }

    #region Login

    [Fact]
    public async Task Email_inexistente_devolve_credencial_invalida()
    {
        _usuarios.ByEmail(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var resultado = await _service.Login(Credencial(), default);

        resultado.Success.ShouldBeFalse();
        resultado.Error!.Code.ShouldBe("invalid_credentials");
    }

    [Fact]
    public async Task Email_inexistente_ainda_assim_gasta_tempo_verificando_para_nao_vazar_a_existencia()
    {
        _usuarios.ByEmail(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        await _service.Login(Credencial(), default);

        _hasher.Received(1).DummyVerify("Senha@123");
    }

    [Fact]
    public async Task Senha_errada_devolve_credencial_invalida()
    {
        _usuarios.ByEmail(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Usuario());
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var resultado = await _service.Login(Credencial(), default);

        resultado.Error!.Code.ShouldBe("invalid_credentials");
    }

    [Fact]
    public async Task Senha_errada_registra_a_tentativa_invalida()
    {
        var usuario = Usuario();
        _usuarios.ByEmail(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        await _service.Login(Credencial(), default);

        usuario.FailedAccessCount.ShouldBe(1);
        await _usuarios.Received(1).SaveChanges(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Usuario_bloqueado_devolve_user_locked_mesmo_com_a_senha_certa()
    {
        var usuario = Usuario();
        for (var tentativa = 0; tentativa < 5; tentativa++)
            usuario.RegisterInvalidAccess(5, TimeSpan.FromMinutes(15));

        _usuarios.ByEmail(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var resultado = await _service.Login(Credencial(), default);

        resultado.Error!.Code.ShouldBe("user_locked");
    }

    [Fact]
    public async Task Login_valido_abre_a_sessao_por_cookie()
    {
        _usuarios.ByEmail(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Usuario());
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var resultado = await _service.Login(Credencial(), default);

        resultado.Success.ShouldBeTrue();
        await _autenticacao.Received(1).SignInAsync(
            Arg.Any<HttpContext>(),
            CookieAuthenticationDefaults.AuthenticationScheme,
            Arg.Any<ClaimsPrincipal>(),
            Arg.Any<AuthenticationProperties>());
    }

    [Fact]
    public async Task Login_valido_limpa_as_tentativas_anteriores()
    {
        var usuario = Usuario();
        usuario.RegisterInvalidAccess(5, TimeSpan.FromMinutes(15));

        _usuarios.ByEmail(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await _service.Login(Credencial(), default);

        usuario.FailedAccessCount.ShouldBe(0);
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_encerra_a_sessao_por_cookie()
    {
        var resultado = await _service.Logout();

        resultado.Success.ShouldBeTrue();
        await _autenticacao.Received(1).SignOutAsync(
            Arg.Any<HttpContext>(),
            CookieAuthenticationDefaults.AuthenticationScheme,
            Arg.Any<AuthenticationProperties>());
    }

    #endregion

    #region Sessão

    [Fact]
    public void Sessao_sem_autenticacao_devolve_invalid_session()
        => _service.Session().Error!.Code.ShouldBe("invalid_session");

    [Fact]
    public void Sessao_autenticada_devolve_nome_email_e_perfis()
    {
        Autenticar("Administrador", "Gerente");

        var resultado = _service.Session();

        resultado.Success.ShouldBeTrue();
        resultado.Value!.Name.ShouldBe("Augusto");
        resultado.Value.Email.ShouldBe("augusto@korp.com.br");
        resultado.Value.Roles.ShouldBe(["Administrador", "Gerente"], ignoreOrder: true);
    }

    #endregion

    #region Token interno

    [Fact]
    public void Token_sem_autenticacao_devolve_invalid_session()
        => _service.Token().Error!.Code.ShouldBe("invalid_session");

    [Fact]
    public void Token_autenticado_devolve_o_jwt_e_a_validade()
    {
        Autenticar("Gerente");
        _tokens.Issue(Arg.Any<ClaimsPrincipal>()).Returns("jwt-interno");

        var resultado = _service.Token();

        resultado.Success.ShouldBeTrue();
        resultado.Value!.Token.ShouldBe("jwt-interno");
        resultado.Value.ExpiresIn.ShouldBe(Gateway.Security.TokenService.SecondsToLive);
    }

    #endregion
}
