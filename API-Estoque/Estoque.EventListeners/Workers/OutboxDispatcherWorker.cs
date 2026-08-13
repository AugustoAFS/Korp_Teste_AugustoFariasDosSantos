using System.Text.Json;
using Estoque.Domain.Interfaces;
using Estoque.EventListeners.Messages.Publicados;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Estoque.EventListeners.Workers
{
    /// <summary>
    /// Lê o outbox e envia ao broker. Entrega ao-menos-uma-vez — por isso
    /// todo consumidor do outro lado precisa ser idempotente.
    /// </summary>
    public sealed class OutboxDispatcherWorker : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(2);
        private const int Lote = 50;

        /// <summary>
        /// O outbox guarda tipo + JSON; para publicar tipado é preciso
        /// resolver o tipo CLR de volta.
        /// </summary>
        private static readonly Dictionary<string, Type> Tipos = new()
        {
            [nameof(ProdutoCriadoEvent)] = typeof(ProdutoCriadoEvent),
            [nameof(ProdutoAtualizadoEvent)] = typeof(ProdutoAtualizadoEvent),
            [nameof(EstoqueBaixadoEvent)] = typeof(EstoqueBaixadoEvent),
            [nameof(EstoqueRejeitadoEvent)] = typeof(EstoqueRejeitadoEvent),
        };

        private readonly IServiceScopeFactory _scopes;
        private readonly IPublishEndpoint _publish;
        private readonly ILogger<OutboxDispatcherWorker> _log;

        public OutboxDispatcherWorker(
            IServiceScopeFactory scopes,
            IPublishEndpoint publish,
            ILogger<OutboxDispatcherWorker> log)
        {
            _scopes = scopes;
            _publish = publish;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(Intervalo);

            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    await Despachar(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Falha no ciclo não derruba o worker — tenta de novo no próximo tick.
                    _log.LogError(ex, "Falha no ciclo do outbox");
                }
            }
        }

        private async Task Despachar(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var pendentes = await outbox.Pendentes(Lote, ct);
            if (pendentes.Count == 0) return;

            foreach (var item in pendentes)
            {
                try
                {
                    if (!Tipos.TryGetValue(item.Tipo, out var tipo))
                    {
                        await outbox.RegistrarFalha(item.Id, $"Tipo desconhecido: {item.Tipo}", ct);
                        continue;
                    }

                    var evento = JsonSerializer.Deserialize(item.Payload, tipo)
                                 ?? throw new InvalidOperationException("Payload nulo");

                    await _publish.Publish(evento, tipo, ct);
                    await outbox.MarcarPublicado(item.Id, ct);
                }
                catch (Exception ex)
                {
                    await outbox.RegistrarFalha(item.Id, ex.Message, ct);
                    _log.LogWarning(ex, "Falha ao publicar mensagem {Id} do outbox", item.Id);
                }
            }
        }
    }
}
