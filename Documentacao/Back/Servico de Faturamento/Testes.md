# Testes — Serviço de Faturamento

xUnit · NSubstitute · Testcontainers (PostgreSQL + RabbitMQ)

```
API-Faturamento/
  Testes/
    TestesUnitarios/     Faturamento.TestesUnitarios.csproj
    TestesIntegracao/    Faturamento.TestesIntegracao.csproj
```

```bash
cd API-Faturamento && dotnet test
```

## Estrutura

```
Faturamento.TestesUnitarios
  Controllers/
    BaseControllerTests.cs
  Services/
    InvoiceServiceTests.cs
    InvoicePrintServiceTests.cs
    ProductReplicationServiceTests.cs
  Entities/
    InvoiceTests.cs
    InvoiceItemTests.cs
    ReplicatedProductTests.cs
    ProcessedMessageTests.cs
    OutboxMessageTests.cs
  EventListeners/
    UrnExchangeNameFormatterTests.cs
    Messages/
      MessageUrnTests.cs

Faturamento.TestesIntegracao
  Suporte/
    PostgresFixture.cs
    RabbitMqFixture.cs
    FaturamentoApiFactory.cs
  Controllers/
    NotasControllerTests.cs
  Services/
    InvoicePrintServiceTests.cs
  Repositories/
    InvoiceRepositoryTests.cs
    ReplicatedProductRepositoryTests.cs
    ProcessedMessageRepositoryTests.cs
    OutboxRepositoryTests.cs
    UnitOfWorkTests.cs
  EventListeners/
    Listeners/
      ConsumidoresTests.cs
    Workers/
      OutboxDispatcherWorkerTests.cs
      PrintExpirationWorkerTests.cs
    Publishers/
      FaturamentoEventPublisherTests.cs
    EventListenerServiceTests.cs
```