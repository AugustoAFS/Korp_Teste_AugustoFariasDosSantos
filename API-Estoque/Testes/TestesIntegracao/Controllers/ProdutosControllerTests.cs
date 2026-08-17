using System.Net;
using System.Net.Http.Json;
using Estoque.Domain.Dtos.Request;
using Estoque.TestesIntegracao.Suporte;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Estoque.TestesIntegracao.Controllers;

public sealed record ProdutoNaResposta(Guid Id, string Code, string Description, int Balance, bool Active);

public sealed record PaginaDeProdutos(
    IReadOnlyList<ProdutoNaResposta> Items, int Page, int Size, int Total, int TotalPages);

[Collection(BancoCollection.Nome)]
public sealed class ProdutosControllerTests : IAsyncLifetime, IDisposable
{
    private const string Rota = "/api/v1/produtos";

    private readonly SqlServerFixture _banco;
    private readonly EstoqueApiFactory _api;

    public ProdutosControllerTests(SqlServerFixture banco, RabbitMqFixture broker)
    {
        _banco = banco;
        _api = new EstoqueApiFactory(banco, broker);
    }

    public async Task InitializeAsync() => await _banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _api.Dispose();

    private HttpClient Gerente() => _api.ClienteComPerfil("Gerente");

    private HttpClient Funcionario() => _api.ClienteComPerfil("Funcionario");

    private static CreateProductRequest NovoProduto(string codigo = "PAR-M8", int saldo = 10)
        => new() { Code = codigo, Description = $"Produto {codigo}", Balance = saldo };

    private async Task<ProdutoNaResposta> Criar(string codigo = "PAR-M8", int saldo = 10)
    {
        var resposta = await Gerente().PostAsJsonAsync(Rota, NovoProduto(codigo, saldo));
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await resposta.Content.ReadFromJsonAsync<ProdutoNaResposta>())!;
    }

    #region Autenticação e autorização

    [Fact]
    public async Task Requisicao_sem_token_recebe_401_com_codigo_de_sessao_invalida()
    {
        var resposta = await _api.ClienteAnonimo().GetAsync(Rota);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("invalid_session");
    }

    [Fact]
    public async Task Funcionario_pode_consultar_produtos()
    {
        var resposta = await Funcionario().GetAsync(Rota);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Funcionario_nao_pode_criar_produto()
    {
        var resposta = await Funcionario().PostAsJsonAsync(Rota, NovoProduto());

        resposta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Funcionario_nao_pode_excluir_produto()
    {
        var produto = await Criar(saldo: 0);

        var resposta = await Funcionario().DeleteAsync($"{Rota}/{produto.Id}");

        resposta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Administrador_tambem_pode_escrever()
    {
        var resposta = await _api.ClienteComPerfil("Administrador").PostAsJsonAsync(Rota, NovoProduto("ADM-1"));

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    #endregion

    #region Criação

    [Fact]
    public async Task Criacao_valida_devolve_201_com_o_produto()
    {
        var produto = await Criar("PAR-M8", 15);

        produto.Code.ShouldBe("PAR-M8");
        produto.Balance.ShouldBe(15);
        produto.Active.ShouldBeTrue();
        produto.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Codigo_repetido_devolve_409_com_o_codigo_do_catalogo()
    {
        await Criar("PAR-M8");

        var resposta = await Gerente().PostAsJsonAsync(Rota, NovoProduto("PAR-M8"));

        resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("product_code_in_use");
    }

    [Fact]
    public async Task Payload_invalido_devolve_400_de_validacao()
    {
        var resposta = await Gerente().PostAsJsonAsync(
            Rota, new CreateProductRequest { Code = "", Description = "x", Balance = -1 });

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Erro_traz_traceId_para_correlacionar_com_o_log()
    {
        var resposta = await Gerente().GetAsync($"{Rota}/{Guid.CreateVersion7()}");

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["traceId"].ShouldNotBeNull();
    }

    #endregion

    #region Consulta

    [Fact]
    public async Task Produto_inexistente_devolve_404()
    {
        var resposta = await Gerente().GetAsync($"{Rota}/{Guid.CreateVersion7()}");

        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("product_not_found");
    }

    [Fact]
    public async Task Consulta_por_id_devolve_o_produto_criado()
    {
        var criado = await Criar("PAR-M8", 7);

        var produto = await Gerente().GetFromJsonAsync<ProdutoNaResposta>($"{Rota}/{criado.Id}");

        produto!.Id.ShouldBe(criado.Id);
        produto.Balance.ShouldBe(7);
    }

    [Fact]
    public async Task Listagem_devolve_pagina_com_total()
    {
        await Criar("A-1");
        await Criar("A-2");

        var pagina = await Gerente().GetFromJsonAsync<PaginaDeProdutos>($"{Rota}?page=1&size=1");

        pagina!.Total.ShouldBe(2);
        pagina.Items.Count.ShouldBe(1);
        pagina.TotalPages.ShouldBe(2);
    }

    [Fact]
    public async Task Listagem_aceita_busca_por_termo()
    {
        await Criar("PAR-M8");
        await Criar("MAR-BOR");

        var pagina = await Gerente().GetFromJsonAsync<PaginaDeProdutos>($"{Rota}?search=MAR");

        pagina!.Total.ShouldBe(1);
        pagina.Items.Single().Code.ShouldBe("MAR-BOR");
    }

    [Fact]
    public async Task Tamanho_de_pagina_acima_do_maximo_e_recusado()
    {
        var resposta = await Gerente().GetAsync($"{Rota}?size=101");

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Alteração e exclusão

    [Fact]
    public async Task Atualizacao_altera_codigo_descricao_e_situacao()
    {
        var produto = await Criar("PAR-M8");

        var resposta = await Gerente().PutAsJsonAsync(
            $"{Rota}/{produto.Id}",
            new UpdateProductRequest { Code = "PAR-M10", Description = "Parafuso M10", Active = false });

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var atualizado = await resposta.Content.ReadFromJsonAsync<ProdutoNaResposta>();
        atualizado!.Code.ShouldBe("PAR-M10");
        atualizado.Active.ShouldBeFalse();
    }

    [Fact]
    public async Task Atualizacao_de_produto_inexistente_devolve_404()
    {
        var resposta = await Gerente().PutAsJsonAsync(
            $"{Rota}/{Guid.CreateVersion7()}",
            new UpdateProductRequest { Code = "X", Description = "Descrição" });

        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Exclusao_de_produto_sem_saldo_devolve_204()
    {
        var produto = await Criar("PAR-M8", saldo: 0);

        var resposta = await Gerente().DeleteAsync($"{Rota}/{produto.Id}");

        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Exclusao_de_produto_com_saldo_devolve_422()
    {
        var produto = await Criar("PAR-M8", saldo: 5);

        var resposta = await Gerente().DeleteAsync($"{Rota}/{produto.Id}");

        resposta.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("product_with_balance");
    }

    [Fact]
    public async Task Produto_excluido_some_da_listagem()
    {
        var produto = await Criar("PAR-M8", saldo: 0);

        await Gerente().DeleteAsync($"{Rota}/{produto.Id}");

        var pagina = await Gerente().GetFromJsonAsync<PaginaDeProdutos>(Rota);
        pagina!.Total.ShouldBe(0);
    }

    #endregion
}
