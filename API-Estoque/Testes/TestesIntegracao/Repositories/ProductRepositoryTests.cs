using Estoque.Domain.Dtos.Request;
using Estoque.Domain.Entities;
using Estoque.InfraStructure.Repositories;
using Estoque.TestesIntegracao.Suporte;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Estoque.TestesIntegracao.Repositories;

[Collection(BancoCollection.Nome)]
public sealed class ProductRepositoryTests(SqlServerFixture banco) : IAsyncLifetime
{
    private const int ViolacaoDeCheckConstraint = 547;

    public async Task InitializeAsync() => await banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Product> Semear(string codigo, int saldo)
    {
        await using var contexto = banco.CreateContext();
        var produto = new Product(codigo, $"Produto {codigo}", saldo);
        contexto.Products.Add(produto);
        await contexto.SaveChangesAsync();
        return produto;
    }

    #region Debit

    [Fact]
    public async Task Debit_subtrai_a_quantidade_e_devolve_o_novo_saldo()
    {
        var produto = await Semear("PAR-M8", 10);

        await using var contexto = banco.CreateContext();
        var saldo = await new ProductRepository(contexto).Debit(produto.Id, 2, default);

        saldo.ShouldBe(8);
    }

    [Fact]
    public async Task Debit_recusa_quando_o_saldo_e_menor_que_a_quantidade()
    {
        var produto = await Semear("PAR-M8", 1);

        await using var contexto = banco.CreateContext();
        var saldo = await new ProductRepository(contexto).Debit(produto.Id, 2, default);

        saldo.ShouldBeNull();
    }

    [Fact]
    public async Task Debit_recusado_nao_altera_o_saldo_gravado()
    {
        var produto = await Semear("PAR-M8", 1);

        await using (var contexto = banco.CreateContext())
            await new ProductRepository(contexto).Debit(produto.Id, 2, default);

        await using var conferencia = banco.CreateContext();
        var atual = await conferencia.Products.AsNoTracking().FirstAsync(p => p.Id == produto.Id);
        atual.Balance.ShouldBe(1);
    }

    [Fact]
    public async Task Debit_aceita_zerar_o_saldo_exatamente()
    {
        var produto = await Semear("PAR-M8", 3);

        await using var contexto = banco.CreateContext();
        var saldo = await new ProductRepository(contexto).Debit(produto.Id, 3, default);

        saldo.ShouldBe(0);
    }

    [Fact]
    public async Task Debit_recusa_produto_inativo_mesmo_com_saldo()
    {
        var produto = await Semear("PAR-M8", 10);

        await using (var contexto = banco.CreateContext())
        {
            var alvo = await contexto.Products.FirstAsync(p => p.Id == produto.Id);
            alvo.Update(alvo.Code, alvo.Description, active: false);
            await contexto.SaveChangesAsync();
        }

        await using var baixa = banco.CreateContext();
        var saldo = await new ProductRepository(baixa).Debit(produto.Id, 1, default);

        saldo.ShouldBeNull();
    }

    [Fact]
    public async Task Debit_de_produto_inexistente_devolve_nulo()
    {
        await using var contexto = banco.CreateContext();

        var saldo = await new ProductRepository(contexto).Debit(Guid.CreateVersion7(), 1, default);

        saldo.ShouldBeNull();
    }

    #endregion

    #region Concorrência — requisito opcional (a)

    [Fact]
    public async Task Saldo_1_disputado_por_duas_baixas_simultaneas_atende_apenas_uma()
    {
        var produto = await Semear("PAR-M8", 1);

        async Task<int?> Baixar()
        {
            await using var contexto = banco.CreateContext();
            return await new ProductRepository(contexto).Debit(produto.Id, 1, default);
        }

        var resultados = await Task.WhenAll(Baixar(), Baixar());

        resultados.Count(saldo => saldo is not null).ShouldBe(1);
        resultados.Count(saldo => saldo is null).ShouldBe(1);
    }

    [Fact]
    public async Task Saldo_1_disputado_termina_zerado_e_nunca_negativo()
    {
        var produto = await Semear("PAR-M8", 1);

        async Task Baixar()
        {
            await using var contexto = banco.CreateContext();
            await new ProductRepository(contexto).Debit(produto.Id, 1, default);
        }

        await Task.WhenAll(Baixar(), Baixar(), Baixar(), Baixar());

        await using var conferencia = banco.CreateContext();
        var atual = await conferencia.Products.AsNoTracking().FirstAsync(p => p.Id == produto.Id);
        atual.Balance.ShouldBe(0);
    }

    [Fact]
    public async Task Dez_baixas_simultaneas_em_saldo_10_consomem_exatamente_o_saldo()
    {
        var produto = await Semear("PAR-M8", 10);

        async Task<int?> Baixar()
        {
            await using var contexto = banco.CreateContext();
            return await new ProductRepository(contexto).Debit(produto.Id, 1, default);
        }

        var resultados = await Task.WhenAll(Enumerable.Range(0, 15).Select(_ => Baixar()));

        resultados.Count(saldo => saldo is not null).ShouldBe(10);

        await using var conferencia = banco.CreateContext();
        var atual = await conferencia.Products.AsNoTracking().FirstAsync(p => p.Id == produto.Id);
        atual.Balance.ShouldBe(0);
    }

    #endregion

    #region Constraint do banco

    [Fact]
    public async Task Banco_recusa_saldo_negativo_mesmo_por_fora_do_repositorio()
    {
        var produto = await Semear("PAR-M8", 1);

        await using var contexto = banco.CreateContext();

        var excecao = await Should.ThrowAsync<SqlException>(async () =>
            await contexto.Products
                .Where(p => p.Id == produto.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(p => p.Balance, -5)));

        excecao.Number.ShouldBe(ViolacaoDeCheckConstraint);
        excecao.Message.ShouldContain("ck_products_balance");
    }

    #endregion

    #region GetPaged

    [Fact]
    public async Task GetPaged_devolve_o_total_independente_da_pagina()
    {
        for (var i = 1; i <= 7; i++) await Semear($"COD-{i:00}", i);

        await using var contexto = banco.CreateContext();
        var (itens, total) = await new ProductRepository(contexto)
            .GetPaged(new ProductFilterRequest { Page = 1, Size = 3 }, default);

        itens.Count.ShouldBe(3);
        total.ShouldBe(7);
    }

    [Fact]
    public async Task GetPaged_respeita_o_deslocamento_da_pagina()
    {
        for (var i = 1; i <= 5; i++) await Semear($"COD-{i:00}", i);

        await using var contexto = banco.CreateContext();
        var repositorio = new ProductRepository(contexto);

        var (primeira, _) = await repositorio.GetPaged(new ProductFilterRequest { Page = 1, Size = 2 }, default);
        var (segunda, _) = await repositorio.GetPaged(new ProductFilterRequest { Page = 2, Size = 2 }, default);

        primeira.Select(p => p.Code).ShouldNotBe(segunda.Select(p => p.Code));
        primeira.Count.ShouldBe(2);
        segunda.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetPaged_filtra_pelo_termo_de_busca()
    {
        await Semear("PAR-M8", 1);
        await Semear("MAR-BOR", 1);

        await using var contexto = banco.CreateContext();
        var (itens, total) = await new ProductRepository(contexto)
            .GetPaged(new ProductFilterRequest { Search = "PAR" }, default);

        total.ShouldBe(1);
        itens.Single().Code.ShouldBe("PAR-M8");
    }

    [Fact]
    public async Task GetPaged_nao_devolve_produto_excluido()
    {
        var produto = await Semear("PAR-M8", 0);

        await using (var contexto = banco.CreateContext())
        {
            var alvo = await contexto.Products.FirstAsync(p => p.Id == produto.Id);
            alvo.Delete();
            await contexto.SaveChangesAsync();
        }

        await using var consulta = banco.CreateContext();
        var (_, total) = await new ProductRepository(consulta).GetPaged(new ProductFilterRequest(), default);

        total.ShouldBe(0);
    }

    #endregion

    #region CodeInUse

    [Fact]
    public async Task CodeInUse_acusa_codigo_ja_cadastrado()
    {
        await Semear("PAR-M8", 1);

        await using var contexto = banco.CreateContext();

        (await new ProductRepository(contexto).CodeInUse("PAR-M8", null, default)).ShouldBeTrue();
    }

    [Fact]
    public async Task CodeInUse_ignora_o_proprio_produto_na_edicao()
    {
        var produto = await Semear("PAR-M8", 1);

        await using var contexto = banco.CreateContext();

        (await new ProductRepository(contexto).CodeInUse("PAR-M8", produto.Id, default)).ShouldBeFalse();
    }

    [Fact]
    public async Task CodeInUse_libera_codigo_inedito()
    {
        await Semear("PAR-M8", 1);

        await using var contexto = banco.CreateContext();

        (await new ProductRepository(contexto).CodeInUse("MAR-BOR", null, default)).ShouldBeFalse();
    }

    #endregion
}
