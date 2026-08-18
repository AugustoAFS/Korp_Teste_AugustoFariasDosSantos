# Testes — Serviço de Estoque

xUnit · NSubstitute · Testcontainers (SQL Server + RabbitMQ)

```
API-Estoque/
  Testes/
    TestesUnitarios/     Estoque.TestesUnitarios.csproj
    TestesIntegracao/    Estoque.TestesIntegracao.csproj
```

```bash
cd API-Estoque && dotnet test
```

## Estrutura

```
Estoque.TestesUnitarios
  Controllers/
    BaseControllerTests.cs
  Services/
    ProductServiceTests.cs
    StockDebitServiceTests.cs
  Entities/
    ProductTests.cs
    StockMovementTests.cs
    ProcessedMessageTests.cs
    OutboxMessageTests.cs
  EventListeners/
    UrnExchangeNameFormatterTests.cs
    Messages/
      MessageUrnTests.cs

Estoque.TestesIntegracao
  Suporte/
    SqlServerFixture.cs
    RabbitMqFixture.cs
    EstoqueApiFactory.cs
    BancoCollection.cs
  Controllers/
    ProdutosControllerTests.cs
  Services/
    StockDebitServiceTests.cs
  Repositories/
    ProductRepositoryTests.cs
    ProcessedMessageRepositoryTests.cs
    StockMovementRepositoryTests.cs
    OutboxRepositoryTests.cs
    UnitOfWorkTests.cs
  EventListeners/
    Listeners/
      OnBaixarEstoqueTests.cs
    Workers/
      OutboxDispatcherWorkerTests.cs
    Publishers/
      EstoqueEventPublisherTests.cs
    EventListenerServiceTests.cs
```
