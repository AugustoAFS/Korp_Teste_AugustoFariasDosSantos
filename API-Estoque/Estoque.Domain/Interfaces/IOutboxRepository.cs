namespace Estoque.Domain.Interfaces
{
    /// <summary>
    /// Outbox transacional. O evento é gravado na mesma transação da mudança
    /// de estado; um worker em background publica depois.
    /// </summary>
    public interface IOutboxRepository
    {
        Task Adicionar(string tipo, string payload, CancellationToken ct);

        Task<IReadOnlyList<OutboxPendente>> Pendentes(int limite, CancellationToken ct);

        Task MarcarPublicado(Guid id, CancellationToken ct);

        Task RegistrarFalha(Guid id, string erro, CancellationToken ct);
    }

    public sealed record OutboxPendente(Guid Id, string Tipo, string Payload);
}
