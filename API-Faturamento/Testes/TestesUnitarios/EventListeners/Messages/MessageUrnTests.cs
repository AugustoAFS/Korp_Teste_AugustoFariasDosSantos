using System.Reflection;
using Faturamento.EventListeners.Messages.Consumidos;
using Faturamento.EventListeners.Messages.Publicados;
using MassTransit;
using Shouldly;

namespace Faturamento.TestesUnitarios.EventListeners.Messages;

public sealed class MessageUrnTests
{
    private static readonly Type[] Contratos =
    [
        typeof(BaixarEstoqueCommand),
        typeof(EstoqueBaixadoEvent),
        typeof(EstoqueRejeitadoEvent),
        typeof(ProdutoCriadoEvent),
        typeof(ProdutoAtualizadoEvent)
    ];

    [Fact]
    public void Todo_contrato_de_mensagem_declara_MessageUrn()
    {
        foreach (var contrato in Contratos)
            contrato.GetCustomAttribute<MessageUrnAttribute>()
                .ShouldNotBeNull($"{contrato.Name} precisa de [MessageUrn] para o contrato valer entre serviços.");
    }

    [Fact]
    public void Nenhum_contrato_repete_o_prefixo_urn_message_no_atributo()
    {
        foreach (var contrato in Contratos)
        {
            var urn = MessageUrn.ForTypeString(contrato);

            urn.ShouldStartWith("urn:message:emissor:");
            urn.ShouldNotStartWith("urn:message:urn:message:");
        }
    }

    [Theory]
    [InlineData(typeof(BaixarEstoqueCommand), "urn:message:emissor:baixar-estoque")]
    [InlineData(typeof(EstoqueBaixadoEvent), "urn:message:emissor:estoque-baixado")]
    [InlineData(typeof(EstoqueRejeitadoEvent), "urn:message:emissor:estoque-rejeitado")]
    [InlineData(typeof(ProdutoCriadoEvent), "urn:message:emissor:produto-criado")]
    [InlineData(typeof(ProdutoAtualizadoEvent), "urn:message:emissor:produto-atualizado")]
    public void Urn_de_cada_contrato_e_exatamente_a_combinada_com_o_estoque(Type contrato, string esperada)
        => MessageUrn.ForTypeString(contrato).ShouldBe(esperada);

    [Fact]
    public void Nenhuma_urn_se_repete_entre_contratos()
        => Contratos.Select(MessageUrn.ForTypeString).Distinct().Count().ShouldBe(Contratos.Length);

    [Fact]
    public void Contrato_e_serializavel_sem_disparar_a_validacao_do_atributo()
    {
        var comando = new BaixarEstoqueCommand
        {
            NotaFiscalId = 42,
            ProcessamentoId = Guid.CreateVersion7(),
            UsuarioId = 7,
            Itens = []
        };

        Should.NotThrow(() => System.Text.Json.JsonSerializer.Serialize(comando));
    }
}
