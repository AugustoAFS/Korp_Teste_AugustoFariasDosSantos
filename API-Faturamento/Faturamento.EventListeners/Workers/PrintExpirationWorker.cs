using Faturamento.ApplicationService.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Faturamento.EventListeners.Workers;

public sealed class PrintExpirationWorker(
    IServiceScopeFactory scopes,
    ILogger<PrintExpirationWorker> log) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan Tolerancia = TimeSpan.FromSeconds(60);
    private const int Lote = 50;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(Intervalo);

        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await Expire(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Falha no ciclo de expiração de impressões");
            }
        }
    }

    private async Task Expire(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();

        var impressao = scope.ServiceProvider.GetRequiredService<IInvoicePrintService>();

        await impressao.ExpirePrintings(Tolerancia, Lote, ct);
    }
}
