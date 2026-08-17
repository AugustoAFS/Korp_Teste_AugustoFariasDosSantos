using Gateway.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Gateway.TestesIntegracao.Suporte;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("gateway")
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

    public GatewayDbContext CreateContext()
        => new(new DbContextOptionsBuilder<GatewayDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options);

    public async Task LimparUsuarios()
    {
        await using var contexto = CreateContext();

        await contexto.Database.ExecuteSqlRawAsync("DELETE FROM user_roles");
        await contexto.Database.ExecuteSqlRawAsync("DELETE FROM users");
    }
}
