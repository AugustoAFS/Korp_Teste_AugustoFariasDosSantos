namespace Gateway.TestesIntegracao.Suporte;

[CollectionDefinition(Nome)]
public sealed class AmbienteCollection : ICollectionFixture<PostgresFixture>
{
    public const string Nome = "ambiente";
}
