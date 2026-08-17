using Faturamento.Domain.Dtos.Request;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;
using Faturamento.InfraStructure.Repositories;
using Faturamento.TestesIntegracao.Suporte;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Faturamento.TestesIntegracao.Repositories;

[Collection(AmbienteCollection.Nome)]
public sealed class InvoiceRepositoryTests(PostgresFixture banco) : IAsyncLifetime
{
    public async Task InitializeAsync() => await banco.Limpar();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Invoice> Semear(
        long emitidaPor = 7, int itens = 1, Action<Invoice>? ajuste = null)
    {
        await using var contexto = banco.CreateContext();

        var numero = await new InvoiceRepository(contexto).NextNumber(default);
        var nota = new Invoice(numero, emitidaPor, "Augusto");

        for (var indice = 0; indice < itens; indice++)
            nota.AddItem(Guid.CreateVersion7(), $"COD-{indice}", $"Produto {indice}", indice + 1);

        ajuste?.Invoke(nota);

        contexto.Invoices.Add(nota);
        await contexto.SaveChangesAsync();

        return nota;
    }

    #region Numeração sequencial

    [Fact]
    public async Task NextNumber_avanca_a_cada_chamada()
    {
        await using var contexto = banco.CreateContext();
        var repositorio = new InvoiceRepository(contexto);

        var primeiro = await repositorio.NextNumber(default);
        var segundo = await repositorio.NextNumber(default);

        segundo.ShouldBe(primeiro + 1);
    }

    [Fact]
    public async Task Numeracao_nao_se_repete_sob_concorrencia()
    {
        async Task<long> Proximo()
        {
            await using var contexto = banco.CreateContext();
            return await new InvoiceRepository(contexto).NextNumber(default);
        }

        var numeros = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Proximo()));

        numeros.Distinct().Count().ShouldBe(20);
    }

    #endregion

    #region Consulta e visibilidade

    [Fact]
    public async Task GetById_traz_os_itens_da_nota()
    {
        var semeada = await Semear(itens: 3);

        await using var contexto = banco.CreateContext();
        var nota = await new InvoiceRepository(contexto).GetById(semeada.Id, default);

        nota!.Items.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Listagem_filtrada_por_usuario_so_traz_as_notas_dele()
    {
        await Semear(emitidaPor: 7);
        await Semear(emitidaPor: 99);

        await using var contexto = banco.CreateContext();
        var (itens, total) = await new InvoiceRepository(contexto)
            .GetPaged(new InvoiceFilterRequest(), onlyUserId: 7, default);

        total.ShouldBe(1);
        itens.Single().IssuedByUserId.ShouldBe(7);
    }

    [Fact]
    public async Task Listagem_sem_filtro_de_usuario_traz_todas()
    {
        await Semear(emitidaPor: 7);
        await Semear(emitidaPor: 99);

        await using var contexto = banco.CreateContext();
        var (_, total) = await new InvoiceRepository(contexto)
            .GetPaged(new InvoiceFilterRequest(), onlyUserId: null, default);

        total.ShouldBe(2);
    }

    [Fact]
    public async Task Listagem_ordena_da_nota_mais_recente_para_a_mais_antiga()
    {
        var primeira = await Semear();
        var segunda = await Semear();

        await using var contexto = banco.CreateContext();
        var (itens, _) = await new InvoiceRepository(contexto)
            .GetPaged(new InvoiceFilterRequest(), null, default);

        itens.First().Number.ShouldBe(segunda.Number);
        itens.Last().Number.ShouldBe(primeira.Number);
    }

    [Fact]
    public async Task Nota_excluida_some_da_listagem()
    {
        var nota = await Semear();

        await using (var contexto = banco.CreateContext())
        {
            var alvo = await contexto.Invoices.FirstAsync(candidata => candidata.Id == nota.Id);
            alvo.Delete();
            await contexto.SaveChangesAsync();
        }

        await using var consulta = banco.CreateContext();
        var (_, total) = await new InvoiceRepository(consulta)
            .GetPaged(new InvoiceFilterRequest(), null, default);

        total.ShouldBe(0);
    }

    #endregion

    #region Filtro de situação

    [Fact]
    public async Task Situacao_aberta_traz_a_nota_sem_processamento_e_sem_erro()
    {
        await Semear();

        await using var contexto = banco.CreateContext();
        var (_, total) = await new InvoiceRepository(contexto)
            .GetPaged(new InvoiceFilterRequest { Situation = InvoiceSituation.Open }, null, default);

        total.ShouldBe(1);
    }

    [Fact]
    public async Task Situacao_imprimindo_traz_apenas_a_nota_com_processamento_em_curso()
    {
        await Semear();
        await Semear(ajuste: nota => nota.StartPrinting(Guid.CreateVersion7()));

        await using var contexto = banco.CreateContext();
        var (_, total) = await new InvoiceRepository(contexto)
            .GetPaged(new InvoiceFilterRequest { Situation = InvoiceSituation.Printing }, null, default);

        total.ShouldBe(1);
    }

    [Fact]
    public async Task Situacao_pendente_traz_a_nota_aberta_que_falhou()
    {
        await Semear();
        await Semear(ajuste: nota =>
        {
            nota.StartPrinting(Guid.CreateVersion7());
            nota.ExpirePrinting("expirou");
        });

        await using var contexto = banco.CreateContext();
        var (_, total) = await new InvoiceRepository(contexto)
            .GetPaged(new InvoiceFilterRequest { Situation = InvoiceSituation.Pending }, null, default);

        total.ShouldBe(1);
    }

    [Fact]
    public async Task Situacao_fechada_traz_apenas_a_nota_impressa()
    {
        await Semear();
        await Semear(ajuste: nota =>
        {
            nota.StartPrinting(Guid.CreateVersion7());
            nota.Close();
        });

        await using var contexto = banco.CreateContext();
        var (_, total) = await new InvoiceRepository(contexto)
            .GetPaged(new InvoiceFilterRequest { Situation = InvoiceSituation.Closed }, null, default);

        total.ShouldBe(1);
    }

    [Fact]
    public async Task Sem_filtro_de_situacao_todas_as_notas_aparecem()
    {
        await Semear();
        await Semear(ajuste: nota => nota.StartPrinting(Guid.CreateVersion7()));
        await Semear(ajuste: nota =>
        {
            nota.StartPrinting(Guid.CreateVersion7());
            nota.Close();
        });

        await using var contexto = banco.CreateContext();
        var (_, total) = await new InvoiceRepository(contexto)
            .GetPaged(new InvoiceFilterRequest(), null, default);

        total.ShouldBe(3);
    }

    #endregion

    #region Reserva da impressão

    [Fact]
    public async Task StartPrinting_reserva_a_nota_aberta()
    {
        var nota = await Semear();

        await using var contexto = banco.CreateContext();

        (await new InvoiceRepository(contexto).StartPrinting(nota.Id, Guid.CreateVersion7(), default))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task StartPrinting_recusa_nota_ja_reservada()
    {
        var nota = await Semear(ajuste: n => n.StartPrinting(Guid.CreateVersion7()));

        await using var contexto = banco.CreateContext();

        (await new InvoiceRepository(contexto).StartPrinting(nota.Id, Guid.CreateVersion7(), default))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Duas_impressoes_simultaneas_reservam_apenas_uma_vez()
    {
        var nota = await Semear();

        async Task<bool> Reservar()
        {
            await using var contexto = banco.CreateContext();
            return await new InvoiceRepository(contexto).StartPrinting(nota.Id, Guid.CreateVersion7(), default);
        }

        var resultados = await Task.WhenAll(Reservar(), Reservar(), Reservar());

        resultados.Count(reservou => reservou).ShouldBe(1);
    }

    [Fact]
    public async Task RestartPrinting_reaproveita_o_processamento_da_nota_expirada()
    {
        var processamento = Guid.CreateVersion7();
        var nota = await Semear(ajuste: n =>
        {
            n.StartPrinting(processamento);
            n.ExpirePrinting("expirou");
        });

        await using var contexto = banco.CreateContext();

        (await new InvoiceRepository(contexto).RestartPrinting(nota.Id, processamento, default))
            .ShouldBeTrue();
    }

    #endregion

    #region Expiração

    [Fact]
    public async Task Expired_ignora_nota_que_acabou_de_comecar_a_imprimir()
    {
        await Semear(ajuste: nota => nota.StartPrinting(Guid.CreateVersion7()));

        await using var contexto = banco.CreateContext();
        var expiradas = await new InvoiceRepository(contexto)
            .Expired(TimeSpan.FromMinutes(5), 50, default);

        expiradas.ShouldBeEmpty();
    }

    [Fact]
    public async Task Expired_encontra_nota_imprimindo_ha_mais_tempo_que_o_limite()
    {
        await Semear(ajuste: nota => nota.StartPrinting(Guid.CreateVersion7()));

        await using var contexto = banco.CreateContext();
        var expiradas = await new InvoiceRepository(contexto)
            .Expired(TimeSpan.Zero, 50, default);

        expiradas.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Expired_ignora_nota_que_ja_tem_erro_registrado()
    {
        await Semear(ajuste: nota =>
        {
            nota.StartPrinting(Guid.CreateVersion7());
            nota.ExpirePrinting("já expirou antes");
        });

        await using var contexto = banco.CreateContext();

        (await new InvoiceRepository(contexto).Expired(TimeSpan.Zero, 50, default)).ShouldBeEmpty();
    }

    #endregion
}
