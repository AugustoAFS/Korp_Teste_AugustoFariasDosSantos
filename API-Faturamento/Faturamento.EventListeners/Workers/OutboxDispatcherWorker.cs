using System.Text.Json;
using Faturamento.Domain.Interfaces;
using Faturamento.EventListeners.Messages.Publicados;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Faturamento.EventListeners.Workers;

public sealed class OutboxDispatcherWorker(
    IServiceScopeFactory scopes,
    IBus bus,
    ILogger<OutboxDispatcherWorker> log) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(2);
    private const int Lote = 50;

    private static readonly Dictionary<string, Type> Tipos = new()
    {
        [nameof(BaixarEstoqueCommand)] = typeof(BaixarEstoqueCommand)
    };

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(Intervalo);

        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await Dispatch(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Falha no ciclo do outbox");
            }
        }
    }

    private async Task Dispatch(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();

        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var pendentes = await outbox.GetPending(Lote, ct);

        foreach (var item in pendentes)
        {
            try
            {
                if (!Tipos.TryGetValue(item.Type, out var tipo))
                {
                    await outbox.RegisterFailure(item.Id, $"Tipo desconhecido: {item.Type}", ct);
                    continue;
                }

                var evento = JsonSerializer.Deserialize(item.Payload, tipo)
                             ?? throw new InvalidOperationException("Payload nulo");

                await bus.Publish(evento, tipo, ct);
                await outbox.MarkPublished(item.Id, ct);
            }
            catch (Exception ex)
            {
                await outbox.RegisterFailure(item.Id, ex.Message, ct);
                log.LogWarning(ex, "Falha ao publicar mensagem {Mensagem} do outbox", item.Id);
            }
        }
    }
}
