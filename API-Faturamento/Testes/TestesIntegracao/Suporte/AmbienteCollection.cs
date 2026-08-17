namespace Faturamento.TestesIntegracao.Suporte;

[CollectionDefinition(Nome)]
public sealed class AmbienteCollection
    : ICollectionFixture<PostgresFixture>, ICollectionFixture<RabbitMqFixture>
{
    public const string Nome = "ambiente";
}
