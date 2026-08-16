using Microsoft.Extensions.Http.Resilience;
using Polly;
using Yarp.ReverseProxy.Forwarder;

namespace Gateway.Config;

public static class ResilienceConfig
{
    private const string ClustersSection = "ReverseProxy:Clusters";
    private const double ProporcaoDeFalha = 0.5;
    private const int MinimoDeRequisicoes = 5;

    private static readonly TimeSpan JanelaDeAmostragem = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan TempoAberto = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(10);

    public static IServiceCollection AddDownstreamResilience(
        this IServiceCollection services, IConfiguration configuration)
    {
        var clusters = configuration.GetSection(ClustersSection)
            .GetChildren()
            .Select(cluster => cluster.Key)
            .Where(id => !string.Equals(id, ReverseProxyConfig.FrontCluster, StringComparison.Ordinal))
            .ToArray();

        if (clusters.Length == 0)
            throw new InvalidOperationException($"{ClustersSection} não configurado.");

        foreach (var cluster in clusters)
            services.AddResiliencePipeline<string, HttpResponseMessage>(cluster, builder => builder
                .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = ProporcaoDeFalha,
                    MinimumThroughput = MinimoDeRequisicoes,
                    SamplingDuration = JanelaDeAmostragem,
                    BreakDuration = TempoAberto
                })
                .AddTimeout(TempoLimite));

        services.AddSingleton<IForwarderHttpClientFactory, ResilientForwarderHttpClientFactory>();

        return services;
    }
}
