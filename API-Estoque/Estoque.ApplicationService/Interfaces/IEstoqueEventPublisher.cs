namespace Estoque.ApplicationService.Interfaces
{
    /// <summary>
    /// Publica os eventos do Estoque. A implementação vive em
    /// Estoque.EventListeners — esta camada não conhece contrato de mensagem.
    /// </summary>
    public interface IEstoqueEventPublisher
    {
        Task ProdutoCriado(
            Guid produtoId, string codigo, string descricao, bool ativo, CancellationToken ct);

        Task ProdutoAtualizado(
            Guid produtoId, string codigo, string descricao, bool ativo, CancellationToken ct);

        Task EstoqueBaixado(
            Guid notaFiscalId,
            Guid processamentoId,
            IReadOnlyList<(Guid ProdutoId, int SaldoNovo)> itens,
            CancellationToken ct);

        Task EstoqueRejeitado(
            Guid notaFiscalId,
            Guid processamentoId,
            Guid produtoId,
            string motivo,
            CancellationToken ct);
    }
}
