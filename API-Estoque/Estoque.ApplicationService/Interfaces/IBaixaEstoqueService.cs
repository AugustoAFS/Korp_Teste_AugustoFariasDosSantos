namespace Estoque.ApplicationService.Interfaces
{
    /// <summary>
    /// Baixa de saldo de uma nota inteira — tudo ou nada.
    /// Se um item faltar saldo, a nota inteira é rejeitada.
    /// </summary>
    public interface IBaixaEstoqueService
    {
        Task<ResultadoBaixa> Baixar(
            Guid notaFiscalId,
            Guid processamentoId,
            Guid? usuarioId,
            IReadOnlyList<ItemBaixa> itens,
            CancellationToken ct);
    }

    public sealed record ItemBaixa(Guid ProdutoId, int Quantidade);

    public sealed record ResultadoBaixa(
        bool Sucesso,
        IReadOnlyList<(Guid ProdutoId, int SaldoNovo)> Itens,
        Guid? ProdutoRejeitado = null,
        string? Motivo = null)
    {
        public static ResultadoBaixa Ok(IReadOnlyList<(Guid, int)> itens) =>
            new(true, itens);

        public static ResultadoBaixa Rejeitado(Guid produtoId, string motivo) =>
            new(false, [], produtoId, motivo);
    }
}
