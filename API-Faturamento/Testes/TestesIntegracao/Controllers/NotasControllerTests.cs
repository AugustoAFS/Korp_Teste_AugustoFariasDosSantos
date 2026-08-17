using System.Net;
using System.Net.Http.Json;
using Faturamento.Domain.Dtos.Request;
using Faturamento.Domain.Entities;
using Faturamento.TestesIntegracao.Suporte;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Faturamento.TestesIntegracao.Controllers;

public sealed record ItemNaResposta(long Id, Guid ProductId, string ProductCode, string ProductDescription, int Quantity);

public sealed record NotaNaResposta(
    long Id,
    long Number,
    string Status,
    string IssuedByUserName,
    bool Printing,
    bool Editable,
    string? LastError,
    IReadOnlyList<ItemNaResposta> Items);

public sealed record PaginaDeNotas(
    IReadOnlyList<NotaNaResposta> Items, int Page, int Size, int Total, int TotalPages);

[Collection(AmbienteCollection.Nome)]
public sealed class NotasControllerTests : IAsyncLifetime, IDisposable
{
    private const string Rota = "/api/v1/notas";

    private readonly PostgresFixture _banco;
    private readonly FaturamentoApiFactory _api;

    public NotasControllerTests(PostgresFixture banco, RabbitMqFixture broker)
    {
        _banco = banco;
        _api = new FaturamentoApiFactory(banco, broker);
    }

    public async Task InitializeAsync() => await _banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _api.Dispose();

    private HttpClient Funcionario(long id = 7) => _api.ClienteComPerfil(id, "Augusto", "Funcionario");

    private HttpClient Gerente() => _api.ClienteComPerfil(1, "Gerente", "Gerente");

    private async Task<Guid> ProdutoReplicado(bool ativo = true)
    {
        var produtoId = Guid.CreateVersion7();

        await using var contexto = _banco.CreateContext();
        contexto.ReplicatedProducts.Add(
            new ReplicatedProduct(produtoId, "PAR-M8", "Parafuso sextavado M8", ativo, DateTimeOffset.UtcNow));
        await contexto.SaveChangesAsync();

        return produtoId;
    }

    private async Task<NotaNaResposta> CriarNota(HttpClient cliente)
    {
        var resposta = await cliente.PostAsync(Rota, null);
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await resposta.Content.ReadFromJsonAsync<NotaNaResposta>())!;
    }

    private static Task<HttpResponseMessage> AdicionarItem(
        HttpClient cliente, long notaId, Guid produtoId, int quantidade = 2)
        => cliente.PostAsJsonAsync(
            $"{Rota}/{notaId}/itens",
            new AddInvoiceItemRequest { ProductId = produtoId, Quantity = quantidade });

    #region Autenticação

    [Fact]
    public async Task Requisicao_sem_token_recebe_401()
    {
        var resposta = await _api.ClienteAnonimo().GetAsync(Rota);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("invalid_session");
    }

    #endregion

    #region Criação e numeração

    [Fact]
    public async Task Nota_nova_nasce_aberta_com_numero_sequencial()
    {
        var primeira = await CriarNota(Funcionario());
        var segunda = await CriarNota(Funcionario());

        primeira.Status.ShouldBe("Open");
        segunda.Number.ShouldBe(primeira.Number + 1);
    }

    [Fact]
    public async Task Status_viaja_como_texto_e_nao_como_numero()
    {
        var nota = await CriarNota(Funcionario());

        var bruto = await Funcionario().GetStringAsync($"{Rota}/{nota.Id}");

        bruto.ShouldContain("\"status\":\"Open\"");
    }

    [Fact]
    public async Task Nota_nova_registra_quem_emitiu()
        => (await CriarNota(Funcionario())).IssuedByUserName.ShouldBe("Augusto");

    #endregion

    #region Visibilidade

    [Fact]
    public async Task Funcionario_nao_enxerga_nota_de_outro_e_recebe_404()
    {
        var nota = await CriarNota(Funcionario(id: 7));

        var resposta = await Funcionario(id: 99).GetAsync($"{Rota}/{nota.Id}");

        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Gerente_enxerga_nota_de_qualquer_um()
    {
        var nota = await CriarNota(Funcionario(id: 7));

        (await Gerente().GetAsync($"{Rota}/{nota.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Listagem_do_funcionario_traz_apenas_as_notas_dele()
    {
        await CriarNota(Funcionario(id: 7));
        await CriarNota(Funcionario(id: 99));

        var pagina = await Funcionario(id: 7).GetFromJsonAsync<PaginaDeNotas>(Rota);

        pagina!.Total.ShouldBe(1);
    }

    [Fact]
    public async Task Listagem_do_gerente_traz_todas_as_notas()
    {
        await CriarNota(Funcionario(id: 7));
        await CriarNota(Funcionario(id: 99));

        var pagina = await Gerente().GetFromJsonAsync<PaginaDeNotas>(Rota);

        pagina!.Total.ShouldBe(2);
    }

    #endregion

    #region Itens

    [Fact]
    public async Task Item_incluido_devolve_a_nota_inteira_com_o_snapshot_do_produto()
    {
        var cliente = Funcionario();
        var nota = await CriarNota(cliente);
        var produto = await ProdutoReplicado();

        var resposta = await AdicionarItem(cliente, nota.Id, produto, 3);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);

        var atualizada = await resposta.Content.ReadFromJsonAsync<NotaNaResposta>();
        var item = atualizada!.Items.Single();
        item.ProductCode.ShouldBe("PAR-M8");
        item.Quantity.ShouldBe(3);
    }

    [Fact]
    public async Task Produto_ainda_nao_replicado_devolve_422_e_pede_para_tentar_depois()
    {
        var cliente = Funcionario();
        var nota = await CriarNota(cliente);

        var resposta = await AdicionarItem(cliente, nota.Id, Guid.CreateVersion7());

        resposta.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Extensions["code"]!.ToString().ShouldBe("product_not_found");
        problema.Detail.ShouldContain("instantes");
    }

    [Fact]
    public async Task Produto_inativo_nao_entra_na_nota()
    {
        var cliente = Funcionario();
        var nota = await CriarNota(cliente);
        var produto = await ProdutoReplicado(ativo: false);

        var resposta = await AdicionarItem(cliente, nota.Id, produto);

        (await resposta.Content.ReadFromJsonAsync<ProblemDetails>())!
            .Extensions["code"]!.ToString().ShouldBe("product_inactive");
    }

    [Fact]
    public async Task Mesmo_produto_duas_vezes_devolve_409()
    {
        var cliente = Funcionario();
        var nota = await CriarNota(cliente);
        var produto = await ProdutoReplicado();

        await AdicionarItem(cliente, nota.Id, produto);
        var resposta = await AdicionarItem(cliente, nota.Id, produto);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Quantidade_zero_e_recusada_na_validacao()
    {
        var cliente = Funcionario();
        var nota = await CriarNota(cliente);
        var produto = await ProdutoReplicado();

        var resposta = await AdicionarItem(cliente, nota.Id, produto, quantidade: 0);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Remocao_de_item_devolve_200_com_a_nota_e_nao_204()
    {
        var cliente = Funcionario();
        var nota = await CriarNota(cliente);
        var produto = await ProdutoReplicado();

        var comItem = await (await AdicionarItem(cliente, nota.Id, produto))
            .Content.ReadFromJsonAsync<NotaNaResposta>();

        var resposta = await cliente.DeleteAsync($"{Rota}/{nota.Id}/itens/{comItem!.Items.Single().Id}");

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var semItem = await resposta.Content.ReadFromJsonAsync<NotaNaResposta>();
        semItem!.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Alteracao_de_quantidade_devolve_a_nota_atualizada()
    {
        var cliente = Funcionario();
        var nota = await CriarNota(cliente);
        var produto = await ProdutoReplicado();

        var comItem = await (await AdicionarItem(cliente, nota.Id, produto, 2))
            .Content.ReadFromJsonAsync<NotaNaResposta>();

        var resposta = await cliente.PutAsJsonAsync(
            $"{Rota}/{nota.Id}/itens/{comItem!.Items.Single().Id}",
            new UpdateInvoiceItemRequest { Quantity = 9 });

        var atualizada = await resposta.Content.ReadFromJsonAsync<NotaNaResposta>();
        atualizada!.Items.Single().Quantity.ShouldBe(9);
    }

    #endregion

    #region Impressão

    [Fact]
    public async Task Nota_sem_item_nao_pode_ser_impressa()
    {
        var cliente = Funcionario();
        var nota = await CriarNota(cliente);

        var resposta = await cliente.PostAsync($"{Rota}/{nota.Id}/impressao", null);

        (await resposta.Content.ReadFromJsonAsync<ProblemDetails>())!
            .Extensions["code"]!.ToString().ShouldBe("invoice_empty");
    }

    [Fact]
    public async Task Impressao_valida_devolve_202_com_a_nota_imprimindo()
    {
        var cliente = Funcionario();
        var nota = await CriarNota(cliente);
        await AdicionarItem(cliente, nota.Id, await ProdutoReplicado());

        var resposta = await cliente.PostAsync($"{Rota}/{nota.Id}/impressao", null);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var imprimindo = await resposta.Content.ReadFromJsonAsync<NotaNaResposta>();
        imprimindo!.Printing.ShouldBeTrue();
        imprimindo.Editable.ShouldBeFalse();
    }

    [Fact]
    public async Task Segunda_impressao_da_mesma_nota_devolve_409()
    {
        var cliente = Funcionario();
        var nota = await CriarNota(cliente);
        await AdicionarItem(cliente, nota.Id, await ProdutoReplicado());

        await cliente.PostAsync($"{Rota}/{nota.Id}/impressao", null);
        var resposta = await cliente.PostAsync($"{Rota}/{nota.Id}/impressao", null);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await resposta.Content.ReadFromJsonAsync<ProblemDetails>())!
            .Extensions["code"]!.ToString().ShouldBe("invoice_already_printing");
    }

    [Fact]
    public async Task Nota_imprimindo_nao_aceita_novo_item()
    {
        var cliente = Funcionario();
        var nota = await CriarNota(cliente);
        await AdicionarItem(cliente, nota.Id, await ProdutoReplicado());
        await cliente.PostAsync($"{Rota}/{nota.Id}/impressao", null);

        var resposta = await AdicionarItem(cliente, nota.Id, await ProdutoReplicado());

        resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    #endregion

    #region Exclusão

    [Fact]
    public async Task Exclusao_de_nota_aberta_devolve_204()
    {
        var cliente = Funcionario();
        var nota = await CriarNota(cliente);

        (await cliente.DeleteAsync($"{Rota}/{nota.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Nota_excluida_some_da_listagem()
    {
        var cliente = Funcionario();
        var nota = await CriarNota(cliente);

        await cliente.DeleteAsync($"{Rota}/{nota.Id}");

        var pagina = await cliente.GetFromJsonAsync<PaginaDeNotas>(Rota);
        pagina!.Total.ShouldBe(0);
    }

    #endregion

    #region Filtro de situação

    [Fact]
    public async Task Filtro_aceita_a_situacao_como_texto()
    {
        var cliente = Funcionario();
        await CriarNota(cliente);

        var pagina = await cliente.GetFromJsonAsync<PaginaDeNotas>($"{Rota}?situation=Open");

        pagina!.Total.ShouldBe(1);
    }

    [Fact]
    public async Task Filtro_de_pendente_existe_e_nao_quebra()
    {
        var cliente = Funcionario();
        await CriarNota(cliente);

        var resposta = await cliente.GetAsync($"{Rota}?situation=Pending");

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Filtro_aceita_a_situacao_como_numero()
    {
        var cliente = Funcionario();
        await CriarNota(cliente);

        var pagina = await cliente.GetFromJsonAsync<PaginaDeNotas>($"{Rota}?situation=1");

        pagina!.Total.ShouldBe(1);
    }

    #endregion
}
