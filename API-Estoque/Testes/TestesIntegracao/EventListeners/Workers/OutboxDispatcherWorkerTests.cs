using System.Reflection;
using System.Text.Json;
using Estoque.EventListeners.Messages.Publicados;
using Estoque.EventListeners.Workers;
using Estoque.InfraStructure.Repositories;
using Estoque.TestesIntegracao.Suporte;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Estoque.TestesIntegracao.EventListeners.Workers;

[Collection(BancoCollection.Nome)]
public sealed class OutboxDispatcherWorkerTests : IAsyncLifetime, IDisposable
{
    private readonly SqlServerFixture _banco;
    private readonly EstoqueApiFactory _api;

    public OutboxDispatcherWorkerTests(SqlServerFixture banco, RabbitMqFixture broker)
    {
        _banco = banco;
        _api = new EstoqueApiFactory(banco, broker);
    }

    public async Task InitializeAsync() => await _banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _api.Dispose();

    #region Mapa de tipos — a armadilha do "Tipo desconhecido"

    private static IReadOnlyDictionary<string, Type> MapaDeTipos()
        => (IReadOnlyDictionary<string, Type>)typeof(OutboxDispatcherWorker)
            .GetField("Tipos", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

    private static Type[] EventosPublicaveis()
        => [.. typeof(ProdutoCriadoEvent).Assembly
            .GetTypes()
            .Where(tipo => tipo.Namespace == typeof(ProdutoCriadoEvent).Namespace)
            .Where(tipo => tipo.GetCustomAttribute<MessageUrnAttribute>() is not null)];

    [Fact]
    public void Todo_evento_publicavel_esta_no_mapa_do_dispatcher()
    {
        var mapa = MapaDeTipos();

        foreach (var evento in EventosPublicaveis())
            mapa.Values.ShouldContain(
                evento,
                $"{evento.Name} não está no mapa do OutboxDispatcherWorker e ficaria preso no outbox.");
    }

    [Fact]
    public void Chave_do_mapa_e_o_nome_do_tipo_gravado_no_outbox()
    {
        foreach (var (nome, tipo) in MapaDeTipos())
            nome.ShouldBe(tipo.Name);
    }

    [Fact]
    public void Mapa_nao_aponta_para_tipo_sem_contrato_de_mensagem()
    {
        foreach (var tipo in MapaDeTipos().Values)
            tipo.GetCustomAttribute<MessageUrnAttribute>().ShouldNotBeNull();
    }

    #endregion

    #region Publicação real

    [Fact]
    public async Task Mensagem_pendente_e_publicada_e_marcada_pelo_worker()
    {
        using var _ = _api.CreateClient();

        var payload = JsonSerializer.Serialize(new ProdutoCriadoEvent
        {
            ProdutoId = Guid.CreateVersion7(),
            Codigo = "PAR-M8",
            Descricao = "Parafuso sextavado M8",
            Ativo = true,
            AtualizadoEm = DateTimeOffset.UtcNow
        });

        Guid id;

        await using (var contexto = _banco.CreateContext())
        {
            await new OutboxRepository(contexto).Add(nameof(ProdutoCriadoEvent), payload, default);
            await contexto.SaveChangesAsync();
            id = await contexto.OutboxMessages.AsNoTracking().Select(m => m.Id).FirstAsync();
        }

        await AguardarPublicacao(id);

        await using var conferencia = _banco.CreateContext();
        var mensagem = await conferencia.OutboxMessages.AsNoTracking().FirstAsync(m => m.Id == id);

        mensagem.PublishedAt.ShouldNotBeNull();
        mensagem.LastError.ShouldBeNull();
    }

    [Fact]
    public async Task Tipo_desconhecido_registra_falha_em_vez_de_travar_o_ciclo()
    {
        using var _ = _api.CreateClient();

        Guid id;

        await using (var contexto = _banco.CreateContext())
        {
            await new OutboxRepository(contexto).Add("EventoQueNaoExiste", "{}", default);
            await contexto.SaveChangesAsync();
            id = await contexto.OutboxMessages.AsNoTracking().Select(m => m.Id).FirstAsync();
        }

        await AguardarFalha(id);

        await using var conferencia = _banco.CreateContext();
        var mensagem = await conferencia.OutboxMessages.AsNoTracking().FirstAsync(m => m.Id == id);

        mensagem.PublishedAt.ShouldBeNull();
        mensagem.LastError.ShouldNotBeNull().ShouldContain("Tipo desconhecido");
        mensagem.Attempts.ShouldBeGreaterThan(0);
    }

    private async Task AguardarPublicacao(Guid id)
    {
        for (var tentativa = 0; tentativa < 20; tentativa++)
        {
            await using var contexto = _banco.CreateContext();
            var publicada = await contexto.OutboxMessages.AsNoTracking()
                .AnyAsync(m => m.Id == id && m.PublishedAt != null);

            if (publicada) return;

            await Task.Delay(500);
        }

        throw new TimeoutException("O worker não publicou a mensagem do outbox a tempo.");
    }

    private async Task AguardarFalha(Guid id)
    {
        for (var tentativa = 0; tentativa < 20; tentativa++)
        {
            await using var contexto = _banco.CreateContext();
            var falhou = await contexto.OutboxMessages.AsNoTracking()
                .AnyAsync(m => m.Id == id && m.LastError != null);

            if (falhou) return;

            await Task.Delay(500);
        }

        throw new TimeoutException("O worker não registrou a falha a tempo.");
    }

    #endregion
}
