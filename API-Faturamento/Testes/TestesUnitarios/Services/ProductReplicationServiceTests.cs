using Faturamento.ApplicationService.Services;
using Faturamento.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Faturamento.TestesUnitarios.Services;

public sealed class ProductReplicationServiceTests
{
    private readonly IReplicatedProductRepository _produtos = Substitute.For<IReplicatedProductRepository>();
    private readonly IProcessedMessageRepository _mensagens = Substitute.For<IProcessedMessageRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ProductReplicationService _service;

    private static readonly Guid Produto = Guid.CreateVersion7();

    public ProductReplicationServiceTests()
    {
        _unitOfWork.SaveWithoutConflict(Arg.Any<CancellationToken>()).Returns(true);

        _service = new ProductReplicationService(
            _produtos, _mensagens, _unitOfWork, NullLogger<ProductReplicationService>.Instance);
    }

    private Task Replicar(Guid? mensagem = null, bool ativo = true)
        => _service.Replicate(
            mensagem ?? Guid.CreateVersion7(),
            "ProdutoCriadoEvent",
            Produto,
            "PAR-M8",
            "Parafuso sextavado M8",
            ativo,
            DateTimeOffset.UtcNow,
            default);

    [Fact]
    public async Task Produto_novo_e_gravado_no_catalogo_replicado()
    {
        await Replicar();

        await _produtos.Received(1).Upsert(
            Produto, "PAR-M8", "Parafuso sextavado M8", true, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).Commit(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Situacao_do_produto_e_replicada_para_bloquear_inclusao_de_inativo()
    {
        await Replicar(ativo: false);

        await _produtos.Received(1).Upsert(
            Produto, Arg.Any<string>(), Arg.Any<string>(), false,
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mensagem_ja_processada_nao_replica_de_novo()
    {
        _mensagens.AlreadyProcessed(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        await Replicar();

        await _produtos.DidNotReceive().Upsert(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).Rollback(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mensagem_marcada_antes_de_replicar_para_a_dedup_valer()
    {
        var mensagem = Guid.CreateVersion7();

        await Replicar(mensagem);

        await _mensagens.Received(1).Mark(mensagem, "ProdutoCriadoEvent", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consumo_concorrente_da_mesma_mensagem_e_abandonado()
    {
        _unitOfWork.SaveWithoutConflict(Arg.Any<CancellationToken>()).Returns(false);

        await Replicar();

        await _produtos.DidNotReceive().Upsert(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).Rollback(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Falha_ao_gravar_desfaz_a_transacao_e_propaga()
    {
        _produtos.Upsert(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("banco fora"));

        await Should.ThrowAsync<InvalidOperationException>(() => Replicar());

        await _unitOfWork.Received(1).Rollback(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().Commit(Arg.Any<CancellationToken>());
    }
}
