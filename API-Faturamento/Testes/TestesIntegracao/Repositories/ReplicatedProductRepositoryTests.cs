using Faturamento.InfraStructure.Repositories;
using Faturamento.TestesIntegracao.Suporte;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Faturamento.TestesIntegracao.Repositories;

[Collection(AmbienteCollection.Nome)]
public sealed class ReplicatedProductRepositoryTests(PostgresFixture banco) : IAsyncLifetime
{
    private static readonly Guid Produto = Guid.CreateVersion7();

    public async Task InitializeAsync() => await banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task Replicar(string codigo, string descricao, bool ativo, DateTimeOffset? momento = null)
    {
        await using var contexto = banco.CreateContext();
        await new ReplicatedProductRepository(contexto)
            .Upsert(Produto, codigo, descricao, ativo, momento ?? DateTimeOffset.UtcNow, default);
        await contexto.SaveChangesAsync();
    }

    [Fact]
    public async Task Produto_inedito_e_inserido_no_catalogo()
    {
        await Replicar("PAR-M8", "Parafuso sextavado M8", true);

        await using var contexto = banco.CreateContext();
        var replica = await new ReplicatedProductRepository(contexto).GetById(Produto, default);

        replica.ShouldNotBeNull();
        replica.Code.ShouldBe("PAR-M8");
        replica.Active.ShouldBeTrue();
    }

    [Fact]
    public async Task Produto_ja_replicado_e_atualizado_em_vez_de_duplicado()
    {
        await Replicar("PAR-M8", "Parafuso sextavado M8", true);
        await Replicar("PAR-M10", "Parafuso sextavado M10", true);

        await using var contexto = banco.CreateContext();

        (await contexto.ReplicatedProducts.AsNoTracking().CountAsync()).ShouldBe(1);

        var replica = await new ReplicatedProductRepository(contexto).GetById(Produto, default);
        replica!.Code.ShouldBe("PAR-M10");
    }

    [Fact]
    public async Task Inativacao_no_estoque_chega_no_catalogo_do_faturamento()
    {
        await Replicar("PAR-M8", "Parafuso", true);
        await Replicar("PAR-M8", "Parafuso", false);

        await using var contexto = banco.CreateContext();
        var replica = await new ReplicatedProductRepository(contexto).GetById(Produto, default);

        replica!.Active.ShouldBeFalse();
    }

    [Fact]
    public async Task Produto_nao_replicado_ainda_devolve_nulo()
    {
        await using var contexto = banco.CreateContext();

        (await new ReplicatedProductRepository(contexto).GetById(Guid.CreateVersion7(), default))
            .ShouldBeNull();
    }
}
