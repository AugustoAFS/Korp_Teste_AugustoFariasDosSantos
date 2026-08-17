using Gateway.Dtos.Request;
using Gateway.Models;
using Gateway.Repositories;
using Gateway.TestesIntegracao.Suporte;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Gateway.TestesIntegracao.Repositories;

[Collection(AmbienteCollection.Nome)]
public sealed class UserRepositoryTests(PostgresFixture banco) : IAsyncLifetime
{
    public async Task InitializeAsync() => await banco.LimparUsuarios();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<User> Semear(string nome, string email, params long[] perfis)
    {
        await using var contexto = banco.CreateContext();

        var usuario = new User(nome, email, "$argon2id$hash");

        foreach (var perfil in perfis)
            usuario.AssignRole(perfil);

        contexto.Users.Add(usuario);
        await contexto.SaveChangesAsync();

        return usuario;
    }

    #region Consulta

    [Fact]
    public async Task ByEmail_encontra_o_usuario_cadastrado()
    {
        await Semear("Augusto", "augusto@korp.com.br");

        await using var contexto = banco.CreateContext();
        var usuario = await new UserRepository(contexto).ByEmail("augusto@korp.com.br", default);

        usuario.ShouldNotBeNull();
        usuario.Name.ShouldBe("Augusto");
    }

    [Fact]
    public async Task ByEmail_ignora_maiuscula_porque_a_coluna_e_citext()
    {
        await Semear("Augusto", "augusto@korp.com.br");

        await using var contexto = banco.CreateContext();

        (await new UserRepository(contexto).ByEmail("AUGUSTO@Korp.Com.Br", default)).ShouldNotBeNull();
    }

    [Fact]
    public async Task Email_com_outra_capitalizacao_e_recusado_como_duplicado()
    {
        await Semear("Augusto", "augusto@korp.com.br");

        await using var contexto = banco.CreateContext();

        (await new UserRepository(contexto).EmailInUse("AUGUSTO@KORP.COM.BR", default)).ShouldBeTrue();
    }

    [Fact]
    public async Task ById_traz_os_perfis_carregados()
    {
        var semeado = await Semear("Augusto", "augusto@korp.com.br", 1, 2);

        await using var contexto = banco.CreateContext();
        var usuario = await new UserRepository(contexto).ById(semeado.Id, default);

        usuario!.Roles.Count.ShouldBe(2);
        usuario.Roles.Select(link => link.Role.Name)
            .ShouldBe(["Administrador", "Gerente"], ignoreOrder: true);
    }

    [Fact]
    public async Task EmailInUse_acusa_email_ja_cadastrado()
    {
        await Semear("Augusto", "augusto@korp.com.br");

        await using var contexto = banco.CreateContext();

        (await new UserRepository(contexto).EmailInUse("augusto@korp.com.br", default)).ShouldBeTrue();
        (await new UserRepository(contexto).EmailInUse("outro@korp.com.br", default)).ShouldBeFalse();
    }

    [Fact]
    public async Task Email_duplicado_e_recusado_pelo_indice_do_banco()
    {
        await Semear("Augusto", "augusto@korp.com.br");

        await using var contexto = banco.CreateContext();
        contexto.Users.Add(new User("Outro", "augusto@korp.com.br", "$argon2id$hash"));

        await Should.ThrowAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    #endregion

    #region Busca com ILIKE

    [Fact]
    public async Task Busca_ignora_maiuscula_e_minuscula_no_nome()
    {
        await Semear("Augusto Farias", "augusto@korp.com.br");

        await using var contexto = banco.CreateContext();
        var (itens, total) = await new UserRepository(contexto)
            .GetPaged(new UserFilterRequest { Search = "AUGUSTO" }, default);

        total.ShouldBe(1);
        itens.Single().Name.ShouldBe("Augusto Farias");
    }

    [Fact]
    public async Task Busca_tambem_alcanca_o_email()
    {
        await Semear("Augusto", "contato@korp.com.br");

        await using var contexto = banco.CreateContext();
        var (_, total) = await new UserRepository(contexto)
            .GetPaged(new UserFilterRequest { Search = "CONTATO" }, default);

        total.ShouldBe(1);
    }

    [Fact]
    public async Task Busca_por_trecho_no_meio_do_texto_funciona()
    {
        await Semear("Augusto Farias", "augusto@korp.com.br");

        await using var contexto = banco.CreateContext();
        var (_, total) = await new UserRepository(contexto)
            .GetPaged(new UserFilterRequest { Search = "Fari" }, default);

        total.ShouldBe(1);
    }

    [Fact]
    public async Task Busca_sem_resultado_devolve_pagina_vazia()
    {
        await Semear("Augusto", "augusto@korp.com.br");

        await using var contexto = banco.CreateContext();
        var (itens, total) = await new UserRepository(contexto)
            .GetPaged(new UserFilterRequest { Search = "ninguem" }, default);

        total.ShouldBe(0);
        itens.ShouldBeEmpty();
    }

    #endregion

    #region Paginação

    [Fact]
    public async Task Paginacao_devolve_o_total_independente_da_pagina()
    {
        for (var indice = 1; indice <= 7; indice++)
            await Semear($"Usuario {indice:00}", $"u{indice}@korp.com.br");

        await using var contexto = banco.CreateContext();
        var (itens, total) = await new UserRepository(contexto)
            .GetPaged(new UserFilterRequest { Page = 1, Size = 3 }, default);

        itens.Count.ShouldBe(3);
        total.ShouldBe(7);
    }

    [Fact]
    public async Task Paginacao_ordena_por_nome_e_avanca_sem_repetir()
    {
        foreach (var nome in new[] { "Carlos", "Ana", "Bruno" })
            await Semear(nome, $"{nome.ToLowerInvariant()}@korp.com.br");

        await using var contexto = banco.CreateContext();
        var repositorio = new UserRepository(contexto);

        var (primeira, _) = await repositorio.GetPaged(new UserFilterRequest { Page = 1, Size = 2 }, default);
        var (segunda, _) = await repositorio.GetPaged(new UserFilterRequest { Page = 2, Size = 2 }, default);

        primeira.Select(u => u.Name).ShouldBe(["Ana", "Bruno"]);
        segunda.Select(u => u.Name).ShouldBe(["Carlos"]);
    }

    [Fact]
    public async Task Listagem_traz_os_perfis_de_cada_usuario()
    {
        await Semear("Augusto", "augusto@korp.com.br", 2);

        await using var contexto = banco.CreateContext();
        var (itens, _) = await new UserRepository(contexto).GetPaged(new UserFilterRequest(), default);

        itens.Single().Roles.Single().Role.Name.ShouldBe("Gerente");
    }

    #endregion
}
