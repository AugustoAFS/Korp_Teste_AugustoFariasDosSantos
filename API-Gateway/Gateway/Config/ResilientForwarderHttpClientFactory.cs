using Microsoft.Extensions.Http.Resilience;
using Polly.Registry;
using Yarp.ReverseProxy.Forwarder;

namespace Gateway.Config;

public sealed class ResilientForwarderHttpClientFactory(ResiliencePipelineProvider<string> pipelines)
    : ForwarderHttpClientFactory
{
    protected override HttpMessageHandler WrapHandler(ForwarderHttpClientContext context, HttpMessageHandler handler)
    {
        var encadeado = base.WrapHandler(context, handler);

        if (string.Equals(context.ClusterId, ReverseProxyConfig.FrontCluster, StringComparison.Ordinal))
            return encadeado;

        return new ResilienceHandler(pipelines.GetPipeline<HttpResponseMessage>(context.ClusterId))
        {
            InnerHandler = encadeado
        };
    }
}
