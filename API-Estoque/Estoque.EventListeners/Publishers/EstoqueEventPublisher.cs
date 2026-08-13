using System.Text.Json;
using Estoque.ApplicationService.Interfaces;
using Estoque.Domain.Interfaces;
using Estoque.EventListeners.Messages.Publicados;

namespace Estoque.EventListeners.Publishers
{
    /// <summary>
    /// Grava o evento no outbox — na mesma transação da mudança de estado.
    /// Quem envia ao broker é o OutboxDispatcherWorker.
    /// </summary>
    public sealed class EstoqueEventPublisher : IEstoqueEventPublisher
    {
        private readonly IOutboxRepository _outbox;

        public EstoqueEventPublisher(IOutboxRepository outbox) => _outbox = outbox;

        public Task ProdutoCriado(
            Guid produtoId, string codigo, string descricao, bool ativo, CancellationToken ct) =>
            Gravar(new ProdutoCriadoEvent(produtoId, codigo, descricao, ativo, DateTimeOffset.UtcNow), ct);

        public Task ProdutoAtualizado(
            Guid produtoId, string codigo, string descricao, bool ativo, CancellationToken ct) =>
            Gravar(new ProdutoAtualizadoEvent(produtoId, codigo, descricao, ativo, DateTimeOffset.UtcNow), ct);

        public Task EstoqueBaixado(
            Guid notaFiscalId,
            Guid processamentoId,
            IReadOnlyList<(Guid ProdutoId, int SaldoNovo)> itens,
            CancellationToken ct)
        {
            var payload = itens.Select(i => new ItemBaixado(i.ProdutoId, i.SaldoNovo)).ToList();
            return Gravar(new EstoqueBaixadoEvent(notaFiscalId, processamentoId, payload), ct);
        }

        public Task EstoqueRejeitado(
            Guid notaFiscalId, Guid processamentoId, Guid produtoId, string motivo, CancellationToken ct) =>
            Gravar(new EstoqueRejeitadoEvent(notaFiscalId, processamentoId, produtoId, motivo), ct);

        private Task Gravar<T>(T evento, CancellationToken ct) where T : notnull =>
            _outbox.Adicionar(typeof(T).Name, JsonSerializer.Serialize(evento), ct);
    }
}
