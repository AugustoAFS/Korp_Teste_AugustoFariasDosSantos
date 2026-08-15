using MassTransit;

namespace Faturamento.EventListeners;

public sealed class UrnExchangeNameFormatter : IEntityNameFormatter
{
    private const string Prefixo = "urn:message:";

    public string FormatEntityName<T>()
    {
        var urn = MessageUrn.ForTypeString<T>();

        return urn.StartsWith(Prefixo, StringComparison.Ordinal) ? urn[Prefixo.Length..] : urn;
    }
}
