namespace Estoque.TestesIntegracao.Suporte;

[CollectionDefinition(Nome)]
public sealed class BancoCollection
    : ICollectionFixture<SqlServerFixture>, ICollectionFixture<RabbitMqFixture>
{
    public const string Nome = "ambiente";
}
