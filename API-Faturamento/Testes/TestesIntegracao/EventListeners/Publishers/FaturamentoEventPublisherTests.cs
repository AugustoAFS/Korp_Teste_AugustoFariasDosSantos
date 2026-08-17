using System.Text.Json;
using Faturamento.Domain.Dtos.EventListeners;
using Faturamento.EventListeners.Messages.Publicados;
using Faturamento.EventListeners.Publishers;
using Faturamento.InfraStructure.Repositories;
using Faturamento.TestesIntegracao.Suporte;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Faturamento.TestesIntegracao.EventListeners.Publishers;

[Collection(AmbienteCollection.Nome)]
public sealed class FaturamentoEventPublisherTests(PostgresFixture banco) : IAsyncLifetime
{
    public async Task InitializeAsync() => await banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(string Tipo, string Payload)> Publicar(
        long notaId, Guid processamento, long? usuario, IReadOnlyList<DebitItem> itens)
    {
        await using var contexto = banco.CreateContext();

        await new FaturamentoEventPublisher(new OutboxRepository(contexto))
            .PublishDebitStock(notaId, processamento, usuario, itens, default);

        await contexto.SaveChangesAsync();

        var gravada = await contexto.OutboxMessages.AsNoTracking().SingleAsync();

        return (gravada.Type, gravada.Payload);
    }

    [Fact]
    public async Task Comando_vai_para_o_outbox_e_nao_direto_para_o_broker()
    {
        var (tipo, _) = await Publicar(42, Guid.CreateVersion7(), 7, []);

        tipo.ShouldBe(nameof(BaixarEstoqueCommand));
    }

    [Fact]
    public async Task Comando_carrega_nota_processamento_e_usuario()
    {
        var processamento = Guid.CreateVersion7();

        var (_, payload) = await Publicar(42, processamento, 7, []);

        var comando = JsonSerializer.Deserialize<BaixarEstoqueCommand>(payload)!;
        comando.NotaFiscalId.ShouldBe(42);
        comando.ProcessamentoId.ShouldBe(processamento);
        comando.UsuarioId.ShouldBe(7);
    }

    [Fact]
    public async Task Comando_carrega_todos_os_itens_da_nota()
    {
        var primeiro = Guid.CreateVersion7();
        var segundo = Guid.CreateVersion7();

        var (_, payload) = await Publicar(
            42,
            Guid.CreateVersion7(),
            7,
            [
                new DebitItem { ProductId = primeiro, Quantity = 2 },
                new DebitItem { ProductId = segundo, Quantity = 3 }
            ]);

        var comando = JsonSerializer.Deserialize<BaixarEstoqueCommand>(payload)!;

        comando.Itens.Count.ShouldBe(2);
        comando.Itens.Select(item => item.ProdutoId).ShouldBe([primeiro, segundo], ignoreOrder: true);
    }

    [Fact]
    public async Task Nota_de_usuario_anonimo_publica_sem_usuario()
    {
        var (_, payload) = await Publicar(42, Guid.CreateVersion7(), null, []);

        JsonSerializer.Deserialize<BaixarEstoqueCommand>(payload)!.UsuarioId.ShouldBeNull();
    }

    [Fact]
    public async Task Tipo_gravado_bate_com_o_mapa_do_dispatcher()
    {
        var (tipo, _) = await Publicar(42, Guid.CreateVersion7(), 7, []);

        tipo.ShouldBe(typeof(BaixarEstoqueCommand).Name);
    }
}
