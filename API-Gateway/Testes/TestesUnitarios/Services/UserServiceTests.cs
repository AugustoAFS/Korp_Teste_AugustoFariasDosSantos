using System.Security.Claims;
using Gateway.Dtos.Request;
using Gateway.Models;
using Gateway.Models.Enums;
using Gateway.Repositories.Interfaces;
using Gateway.Security.Interfaces;
using Gateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Gateway.TestesUnitarios.Services;

public sealed class UserServiceTests
{
    private readonly IUserRepository _usuarios = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _perfis = Substitute.For<IRoleRepository>();
    private readonly IArgon2idHasher _hasher = Substitute.For<IArgon2idHasher>();
    private readonly IHttpContextAccessor _accessor = Substitute.For<IHttpContextAccessor>();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _accessor.HttpContext.Returns(new DefaultHttpContext());
        _hasher.Hash(Arg.Any<string>()).Returns("$argon2id$hash");

        _service = new UserService(
            _usuarios, _perfis, _hasher, _accessor, NullLogger<UserService>.Instance);
    }

    private void Autenticar(long id, params string[] perfisDoUsuario)
    {
        var identidade = new ClaimsIdentity("cookie", ClaimTypes.Name, ClaimTypes.Role);
        identidade.AddClaim(new Claim(ClaimTypes.NameIdentifier, id.ToString()));

        foreach (var perfil in perfisDoUsuario)
            identidade.AddClaim(new Claim(ClaimTypes.Role, perfil));

        _accessor.HttpContext!.User = new ClaimsPrincipal(identidade);
    }

    private static User Usuario() => new("Augusto", "augusto@korp.com.br", "$argon2id$hash");

    private void PerfisExistentes(params string[] nomes)
        => _perfis.ByNames(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Role>)[.. nomes.Select((nome, indice) => new Role(indice + 1, nome, null))]);

    private static CreateUserRequest NovoUsuario(params string[] perfis)
        => new()
        {
            Name = "Augusto",
            Email = "augusto@korp.com.br",
            Password = "Senha@123",
            Roles = perfis
        };

    #region Visibilidade

    [Fact]
    public async Task Usuario_comum_nao_ve_o_cadastro_de_outro()
    {
        Autenticar(7, nameof(DefaultRole.Funcionario));

        var resultado = await _service.ById(99, default);

        resultado.Success.ShouldBeFalse();
        resultado.Error!.Code.ShouldBe("forbidden");
    }

    [Fact]
    public async Task Usuario_comum_ve_o_proprio_cadastro()
    {
        Autenticar(7, nameof(DefaultRole.Funcionario));
        _usuarios.ById(7, Arg.Any<CancellationToken>()).Returns(Usuario());

        var resultado = await _service.ById(7, default);

        resultado.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task Administrador_ve_o_cadastro_de_qualquer_um()
    {
        Autenticar(1, nameof(DefaultRole.Administrador));
        _usuarios.ById(99, Arg.Any<CancellationToken>()).Returns(Usuario());

        var resultado = await _service.ById(99, default);

        resultado.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task Usuario_inexistente_devolve_user_not_found()
    {
        Autenticar(1, nameof(DefaultRole.Administrador));
        _usuarios.ById(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var resultado = await _service.ById(99, default);

        resultado.Error!.Code.ShouldBe("user_not_found");
    }

    #endregion

    #region Criação

    [Fact]
    public async Task Cadastro_aberto_sempre_cria_funcionario_mesmo_pedindo_administrador()
    {
        Autenticar(0);
        PerfisExistentes(nameof(DefaultRole.Funcionario));

        await _service.Create(NovoUsuario(nameof(DefaultRole.Administrador)), default);

        await _perfis.Received(1).ByNames(
            Arg.Is<IEnumerable<string>>(nomes => nomes.Single() == nameof(DefaultRole.Funcionario)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Administrador_pode_escolher_os_perfis_do_novo_usuario()
    {
        Autenticar(1, nameof(DefaultRole.Administrador));
        PerfisExistentes(nameof(DefaultRole.Gerente));

        await _service.Create(NovoUsuario(nameof(DefaultRole.Gerente)), default);

        await _perfis.Received(1).ByNames(
            Arg.Is<IEnumerable<string>>(nomes => nomes.Single() == nameof(DefaultRole.Gerente)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Perfil_inexistente_devolve_role_not_found()
    {
        Autenticar(1, nameof(DefaultRole.Administrador));
        _perfis.ByNames(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Role>)[]);

        var resultado = await _service.Create(NovoUsuario("PerfilQueNaoExiste"), default);

        resultado.Error!.Code.ShouldBe("role_not_found");
    }

    [Fact]
    public async Task Email_ja_cadastrado_devolve_email_in_use()
    {
        Autenticar(0);
        PerfisExistentes(nameof(DefaultRole.Funcionario));
        _usuarios.EmailInUse(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var resultado = await _service.Create(NovoUsuario(), default);

        resultado.Error!.Code.ShouldBe("email_in_use");
        await _usuarios.DidNotReceive().Add(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Criacao_valida_grava_a_senha_com_hash_e_devolve_201()
    {
        Autenticar(0);
        PerfisExistentes(nameof(DefaultRole.Funcionario));

        var resultado = await _service.Create(NovoUsuario(), default);

        resultado.Success.ShouldBeTrue();
        resultado.Status.ShouldBe(StatusCodes.Status201Created);
        _hasher.Received(1).Hash("Senha@123");
        await _usuarios.Received(1).Add(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Senha_em_texto_puro_nunca_chega_na_entidade()
    {
        Autenticar(0);
        PerfisExistentes(nameof(DefaultRole.Funcionario));

        await _service.Create(NovoUsuario(), default);

        await _usuarios.Received(1).Add(
            Arg.Is<User>(usuario => usuario.PasswordHash != "Senha@123"), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Troca de perfis

    [Fact]
    public async Task Troca_de_perfis_de_usuario_inexistente_devolve_user_not_found()
    {
        _usuarios.ById(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var resultado = await _service.ReplaceRoles(
            99, new AssignRolesRequest { Roles = [nameof(DefaultRole.Gerente)] }, default);

        resultado.Error!.Code.ShouldBe("user_not_found");
    }

    [Fact]
    public async Task Troca_de_perfis_com_perfil_inexistente_e_recusada()
    {
        _usuarios.ById(7, Arg.Any<CancellationToken>()).Returns(Usuario());
        _perfis.ByNames(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Role>)[]);

        var resultado = await _service.ReplaceRoles(
            7, new AssignRolesRequest { Roles = ["Inexistente"] }, default);

        resultado.Error!.Code.ShouldBe("role_not_found");
    }

    [Fact]
    public async Task Troca_de_perfis_valida_substitui_e_devolve_204()
    {
        var usuario = Usuario();
        usuario.AssignRole(3);

        _usuarios.ById(7, Arg.Any<CancellationToken>()).Returns(usuario);
        PerfisExistentes(nameof(DefaultRole.Gerente));

        var resultado = await _service.ReplaceRoles(
            7, new AssignRolesRequest { Roles = [nameof(DefaultRole.Gerente)] }, default);

        resultado.Success.ShouldBeTrue();
        resultado.Status.ShouldBe(StatusCodes.Status204NoContent);
        await _usuarios.Received(1).SaveChanges(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Perfis_repetidos_no_pedido_sao_deduplicados()
    {
        _usuarios.ById(7, Arg.Any<CancellationToken>()).Returns(Usuario());
        PerfisExistentes(nameof(DefaultRole.Gerente));

        await _service.ReplaceRoles(
            7,
            new AssignRolesRequest { Roles = [nameof(DefaultRole.Gerente), nameof(DefaultRole.Gerente)] },
            default);

        await _perfis.Received(1).ByNames(
            Arg.Is<IEnumerable<string>>(nomes => nomes.Count() == 1), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Listagem

    [Fact]
    public async Task Listagem_devolve_a_pagina_com_o_total_do_repositorio()
    {
        _usuarios.GetPaged(Arg.Any<UserFilterRequest>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)[Usuario()], 42));

        var resultado = await _service.GetUsers(new UserFilterRequest { Page = 2, Size = 10 }, default);

        resultado.Success.ShouldBeTrue();
        resultado.Value!.Total.ShouldBe(42);
        resultado.Value.Page.ShouldBe(2);
        resultado.Value.Items.Count.ShouldBe(1);
    }

    #endregion
}
