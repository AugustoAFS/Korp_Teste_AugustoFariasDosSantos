using Faturamento.ApplicationService.Services;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;
using Faturamento.Domain.Interfaces;
using Faturamento.EventListeners.Publishers;
using Faturamento.InfraStructure.Data;
using Faturamento.InfraStructure.Repositories;
using Faturamento.TestesIntegracao.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Faturamento.TestesIntegracao.Services;

[Collection(AmbienteCollection.Nome)]
public sealed class InvoicePrintServiceTests(PostgresFixture banco) : IAsyncLifetime
{
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IRejectionExplainer _explicador = Substitute.For<IRejectionExplainer>();

    public async Task InitializeAsync()
    {
        await banco.Limpar();
        _usuario.Id.Returns(7L);
        _usuario.Name.Returns("Augusto");
        _usuario.SeesEveryInvoice.Returns(false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private InvoicePrintService Servico(FaturamentoDbContext contexto)
        => new(
            new InvoiceRepository(contexto),
            new ProcessedMessageRepository(contexto),
            _explicador,
            new FaturamentoEventPublisher(new OutboxRepository(contexto)),
            new UnitOfWork(contexto),
            _usuario,
            NullLogger<InvoicePrintService>.Instance);

    private async Task<Invoice> SemearNotaComItem()
    {
        await using var contexto = banco.CreateContext();

        var numero = await new InvoiceRepository(contexto).NextNumber(default);
        var nota = new Invoice(numero, 7, "Augusto");
        nota.AddItem(Guid.CreateVersion7(), "PAR-M8", "Parafuso sextavado M8", 2);

        contexto.Invoices.Add(nota);
        await contexto.SaveChangesAsync();

        return nota;
    }

    private async Task<Invoice> Recarregar(long id)
    {
        await using var contexto = banco.CreateContext();
        return await contexto.Invoices.AsNoTracking().FirstAsync(nota => nota.Id == id);
    }

    #region Impressão publica no outbox

    [Fact]
    public async Task Impressao_enfileira_o_comando_de_baixa_no_outbox()
    {
        var nota = await SemearNotaComItem();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).PrintInvoice(nota.Id, default);

        await using var conferencia = banco.CreateContext();
        var mensagens = await conferencia.OutboxMessages.AsNoTracking().ToListAsync();

        mensagens.ShouldContain(mensagem => mensagem.Type == "BaixarEstoqueCommand");
    }

    [Fact]
    public async Task Impressao_marca_a_nota_como_imprimindo_no_banco()
    {
        var nota = await SemearNotaComItem();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).PrintInvoice(nota.Id, default);

        var atual = await Recarregar(nota.Id);
        atual.ProcessingId.ShouldNotBeNull();
        atual.LastError.ShouldBeNull();
    }

    [Fact]
    public async Task Segunda_impressao_simultanea_e_recusada_pelo_banco()
    {
        var nota = await SemearNotaComItem();

        async Task<bool> Imprimir()
        {
            await using var contexto = banco.CreateContext();
            return (await Servico(contexto).PrintInvoice(nota.Id, default)).Success;
        }

        var resultados = await Task.WhenAll(Imprimir(), Imprimir());

        resultados.Count(sucesso => sucesso).ShouldBe(1);
    }

    #endregion

    #region Desfecho do saga

    [Fact]
    public async Task Estoque_baixado_fecha_a_nota_no_banco()
    {
        var nota = await SemearNotaComItem();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).PrintInvoice(nota.Id, default);

        var processamento = (await Recarregar(nota.Id)).ProcessingId!.Value;

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).CloseInvoice(Guid.CreateVersion7(), nota.Id, processamento, default);

        var atual = await Recarregar(nota.Id);
        atual.Status.ShouldBe(InvoiceStatus.Closed);
        atual.ClosedAt.ShouldNotBeNull();
        atual.ProcessingId.ShouldBeNull();
    }

    [Fact]
    public async Task Estoque_rejeitado_devolve_a_nota_para_aberta_com_o_motivo()
    {
        var nota = await SemearNotaComItem();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).PrintInvoice(nota.Id, default);

        var processamento = (await Recarregar(nota.Id)).ProcessingId!.Value;

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).RejectInvoice(
                Guid.CreateVersion7(), nota.Id, processamento, "Saldo insuficiente do produto PAR-M8.", default);

        var atual = await Recarregar(nota.Id);
        atual.Status.ShouldBe(InvoiceStatus.Open);
        atual.LastError.ShouldBe("Saldo insuficiente do produto PAR-M8.");
        atual.ProcessingId.ShouldBeNull();
    }

    #endregion

    #region Idempotência do consumo

    [Fact]
    public async Task Mesma_mensagem_entregue_duas_vezes_nao_muda_a_nota_de_novo()
    {
        var nota = await SemearNotaComItem();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).PrintInvoice(nota.Id, default);

        var processamento = (await Recarregar(nota.Id)).ProcessingId!.Value;
        var mensagem = Guid.CreateVersion7();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).CloseInvoice(mensagem, nota.Id, processamento, default);

        var fechadaEm = (await Recarregar(nota.Id)).ClosedAt;

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).CloseInvoice(mensagem, nota.Id, processamento, default);

        (await Recarregar(nota.Id)).ClosedAt.ShouldBe(fechadaEm);
    }

    [Fact]
    public async Task Consumo_duplicado_grava_um_unico_marcador()
    {
        var nota = await SemearNotaComItem();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).PrintInvoice(nota.Id, default);

        var processamento = (await Recarregar(nota.Id)).ProcessingId!.Value;
        var mensagem = Guid.CreateVersion7();

        for (var entrega = 0; entrega < 3; entrega++)
            await using (var contexto = banco.CreateContext())
                await Servico(contexto).CloseInvoice(mensagem, nota.Id, processamento, default);

        await using var conferencia = banco.CreateContext();
        (await conferencia.ProcessedMessages.AsNoTracking().CountAsync(m => m.MessageId == mensagem))
            .ShouldBe(1);
    }

    [Fact]
    public async Task Resultado_de_processamento_antigo_nao_altera_a_nota()
    {
        var nota = await SemearNotaComItem();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).PrintInvoice(nota.Id, default);

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).CloseInvoice(
                Guid.CreateVersion7(), nota.Id, Guid.CreateVersion7(), default);

        (await Recarregar(nota.Id)).Status.ShouldBe(InvoiceStatus.Open);
    }

    #endregion

    #region Auto-cura: expiração preserva o processamento

    [Fact]
    public async Task Expiracao_registra_o_erro_e_preserva_o_processing_id()
    {
        var nota = await SemearNotaComItem();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).PrintInvoice(nota.Id, default);

        var processamento = (await Recarregar(nota.Id)).ProcessingId!.Value;

        await using (var contexto = banco.CreateContext())
            (await Servico(contexto).ExpirePrintings(TimeSpan.Zero, 50, default)).ShouldBe(1);

        var atual = await Recarregar(nota.Id);
        atual.LastError.ShouldNotBeNullOrWhiteSpace();
        atual.ProcessingId.ShouldBe(processamento);
    }

    [Fact]
    public async Task Resultado_atrasado_ainda_fecha_a_nota_expirada()
    {
        var nota = await SemearNotaComItem();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).PrintInvoice(nota.Id, default);

        var processamento = (await Recarregar(nota.Id)).ProcessingId!.Value;

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).ExpirePrintings(TimeSpan.Zero, 50, default);

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).CloseInvoice(Guid.CreateVersion7(), nota.Id, processamento, default);

        var atual = await Recarregar(nota.Id);
        atual.Status.ShouldBe(InvoiceStatus.Closed);
        atual.LastError.ShouldBeNull();
    }

    [Fact]
    public async Task Retentativa_do_usuario_republica_sob_a_mesma_chave()
    {
        var nota = await SemearNotaComItem();

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).PrintInvoice(nota.Id, default);

        var processamento = (await Recarregar(nota.Id)).ProcessingId!.Value;

        await using (var contexto = banco.CreateContext())
            await Servico(contexto).ExpirePrintings(TimeSpan.Zero, 50, default);

        await using (var contexto = banco.CreateContext())
            (await Servico(contexto).PrintInvoice(nota.Id, default)).Success.ShouldBeTrue();

        (await Recarregar(nota.Id)).ProcessingId.ShouldBe(processamento);

        await using var conferencia = banco.CreateContext();
        (await conferencia.OutboxMessages.AsNoTracking().CountAsync(m => m.Type == "BaixarEstoqueCommand"))
            .ShouldBe(2);
    }

    #endregion
}
