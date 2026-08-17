using Faturamento.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Faturamento.TestesIntegracao.Suporte;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("faturamento")
        .WithUsername("emissor")
        .WithPassword("emissor")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var contexto = CreateContext();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public FaturamentoDbContext CreateContext()
        => new(new DbContextOptionsBuilder<FaturamentoDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options);

    public async Task Limpar()
    {
        await using var contexto = CreateContext();

        await contexto.Database.ExecuteSqlRawAsync("DELETE FROM invoice_items");
        await contexto.Database.ExecuteSqlRawAsync("DELETE FROM invoices");
        await contexto.Database.ExecuteSqlRawAsync("DELETE FROM replicated_products");
        await contexto.Database.ExecuteSqlRawAsync("DELETE FROM processed_messages");
        await contexto.Database.ExecuteSqlRawAsync("DELETE FROM outbox_messages");
    }
}
