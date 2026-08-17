using System.Reflection;
using Estoque.ApplicationService.Interfaces;
using Estoque.Domain.Dtos.EventListeners;
using Estoque.EventListeners.Listeners;
using Estoque.EventListeners.Messages.Consumidos;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Estoque.TestesIntegracao.EventListeners.Listeners;

public sealed class OnBaixarEstoqueTests
{
    private readonly IStockDebitService _baixa = Substitute.For<IStockDebitService>();
    private readonly OnBaixarEstoque _consumidor;

    public OnBaixarEstoqueTests()
        => _consumidor = new OnBaixarEstoque(_baixa, NullLogger<OnBaixarEstoque>.Instance);

    private static ConsumeContext<BaixarEstoqueCommand> Contexto(BaixarEstoqueCommand mensagem)
    {
        var contexto = Substitute.For<ConsumeContext<BaixarEstoqueCommand>>();
        contexto.Message.Returns(mensagem);
        contexto.CancellationToken.Returns(CancellationToken.None);
        return contexto;
    }

    private static BaixarEstoqueCommand Comando(params (Guid Produto, int Quantidade)[] itens)
        => new()
        {
            NotaFiscalId = 42,
            ProcessamentoId = Guid.CreateVersion7(),
            UsuarioId = 7,
            Itens = [.. itens.Select(item => new ItemBaixaEstoque
            {
                ProdutoId = item.Produto,
                Quantidade = item.Quantidade
            })]
        };

    [Fact]
    public async Task Consumidor_repassa_nota_processamento_e_usuario_para_o_servico()
    {
        var comando = Comando((Guid.CreateVersion7(), 2));

        await _consumidor.Consume(Contexto(comando));

        await _baixa.Received(1).DebitStock(
            comando.NotaFiscalId,
            comando.ProcessamentoId,
            comando.UsuarioId,
            Arg.Any<IReadOnlyList<DebitItem>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consumidor_traduz_o_contrato_da_mensagem_para_o_dto_de_dominio()
    {
        var produto = Guid.CreateVersion7();

        await _consumidor.Consume(Contexto(Comando((produto, 3))));

        await _baixa.Received(1).DebitStock(
            Arg.Any<long>(),
            Arg.Any<Guid>(),
            Arg.Any<long?>(),
            Arg.Is<IReadOnlyList<DebitItem>>(itens =>
                itens.Count == 1 && itens[0].ProductId == produto && itens[0].Quantity == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consumidor_preserva_todos_os_itens_da_nota()
    {
        var comando = Comando((Guid.CreateVersion7(), 1), (Guid.CreateVersion7(), 2), (Guid.CreateVersion7(), 3));

        await _consumidor.Consume(Contexto(comando));

        await _baixa.Received(1).DebitStock(
            Arg.Any<long>(),
            Arg.Any<Guid>(),
            Arg.Any<long?>(),
            Arg.Is<IReadOnlyList<DebitItem>>(itens => itens.Count == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Falha_do_servico_sobe_para_o_MassTransit_reprocessar()
    {
        _baixa.DebitStock(
                Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<long?>(),
                Arg.Any<IReadOnlyList<DebitItem>>(), Arg.Any<CancellationToken>())
            .Returns<DebitResult>(_ => throw new InvalidOperationException("banco fora"));

        await Should.ThrowAsync<InvalidOperationException>(
            () => _consumidor.Consume(Contexto(Comando((Guid.CreateVersion7(), 1)))));
    }

    #region Definição do endpoint

    [Fact]
    public void Fila_do_consumidor_tem_nome_estavel()
    {
        IConsumerDefinition definicao = new OnBaixarEstoque.Definition();

        definicao.GetEndpointName(DefaultEndpointNameFormatter.Instance)
            .ShouldBe("estoque.on-baixar-estoque");
    }

    [Fact]
    public void Consumidor_processa_em_paralelo_porque_o_banco_resolve_a_concorrencia()
        => new OnBaixarEstoque.Definition().ConcurrentMessageLimit.ShouldBe(5);

    #endregion
}
