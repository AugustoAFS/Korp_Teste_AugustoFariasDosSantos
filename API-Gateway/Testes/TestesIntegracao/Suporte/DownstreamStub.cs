using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gateway.TestesIntegracao.Suporte;

public sealed class DownstreamStub : IAsyncDisposable
{
    private readonly WebApplication _app;

    public DownstreamStub()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        _app = builder.Build();

        _app.Map("/{**resto}", async contexto =>
        {
            Interlocked.Increment(ref _chamadas);

            if (Falhando)
            {
                contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return;
            }

            if (Demora > TimeSpan.Zero)
                await Task.Delay(Demora, contexto.RequestAborted);

            contexto.Response.StatusCode = StatusCodes.Status200OK;
            await contexto.Response.WriteAsync("""{"ok":true}""");
        });
    }

    private int _chamadas;

    public bool Falhando { get; set; }

    public TimeSpan Demora { get; set; } = TimeSpan.Zero;

    public int Chamadas => Volatile.Read(ref _chamadas);

    public string Endereco { get; private set; } = string.Empty;

    public void ZerarContador() => Interlocked.Exchange(ref _chamadas, 0);

    public async Task Iniciar()
    {
        await _app.StartAsync();

        Endereco = _app.Urls.First();
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
