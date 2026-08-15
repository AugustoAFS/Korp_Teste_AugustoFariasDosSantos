using System.Text.Json;
using Faturamento.ApplicationService.Interfaces;
using Faturamento.Domain.Dtos.EventListeners;
using Faturamento.Domain.Interfaces;
using Faturamento.EventListeners.Messages.Publicados;

namespace Faturamento.EventListeners.Publishers;

public sealed class FaturamentoEventPublisher(IOutboxRepository outbox) : IFaturamentoEventPublisher
{
    public Task PublishDebitStock(
        long notaFiscalId,
        Guid processamentoId,
        long? usuarioId,
        IReadOnlyList<DebitItem> itens,
        CancellationToken ct)
        => Store(
            new BaixarEstoqueCommand
            {
                NotaFiscalId = notaFiscalId,
                ProcessamentoId = processamentoId,
                UsuarioId = usuarioId,
                Itens = [.. itens.Select(item => new ItemBaixaEstoque
                {
                    ProdutoId = item.ProductId,
                    Quantidade = item.Quantity
                })]
            },
            ct);

    private Task Store<T>(T evento, CancellationToken ct) where T : notnull
        => outbox.Add(typeof(T).Name, JsonSerializer.Serialize(evento), ct);
}
