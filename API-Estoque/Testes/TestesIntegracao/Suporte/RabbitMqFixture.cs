using Testcontainers.RabbitMq;

namespace Estoque.TestesIntegracao.Suporte;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder()
        .WithImage("rabbitmq:4-management-alpine")
        .WithUsername("Admin")
        .WithPassword("Admin")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
