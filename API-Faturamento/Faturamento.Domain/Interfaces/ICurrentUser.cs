namespace Faturamento.Domain.Interfaces;

public interface ICurrentUser
{
    long? Id { get; }

    string Name { get; }

    bool SeesEveryInvoice { get; }
}
