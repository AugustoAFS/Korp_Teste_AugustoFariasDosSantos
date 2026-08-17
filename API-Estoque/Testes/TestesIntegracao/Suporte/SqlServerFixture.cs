using Estoque.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace Estoque.TestesIntegracao.Suporte;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var contexto = CreateContext();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public EstoqueDbContext CreateContext()
        => new(new DbContextOptionsBuilder<EstoqueDbContext>()
            .UseSqlServer(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options);

    public async Task Limpar()
    {
        await using var contexto = CreateContext();

        await contexto.Database.ExecuteSqlRawAsync("DELETE FROM stock_movements");
        await contexto.Database.ExecuteSqlRawAsync("DELETE FROM processed_messages");
        await contexto.Database.ExecuteSqlRawAsync("DELETE FROM outbox_messages");
        await contexto.Database.ExecuteSqlRawAsync("DELETE FROM products");
    }
}
