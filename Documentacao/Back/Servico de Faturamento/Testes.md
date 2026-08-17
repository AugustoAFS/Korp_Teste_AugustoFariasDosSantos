# Testes — Serviço de Faturamento

xUnit · NSubstitute · Testcontainers (PostgreSQL + RabbitMQ)

## Localização

Os dois projetos ficam dentro do próprio serviço e entram em `API-Faturamento.slnx`. Não há projeto de teste na raiz do repositório: um projeto que referenciasse os três serviços acoplaria os microsserviços em tempo de build.

```
API-Faturamento/
  Testes/
    TestesUnitarios/     Faturamento.TestesUnitarios.csproj
    TestesIntegracao/    Faturamento.TestesIntegracao.csproj
```

## Convenção

A organização segue a **sequência de execução**, não as camadas da Clean Architecture:

```
Controllers/  →  Services/  →  Repositories/
```

O nome do arquivo espelha o do projeto com o sufixo `Tests`. A camada de origem é descartada — `Faturamento.InfraStructure/Repositories/InvoiceRepository.cs` vira `Repositories/InvoiceRepositoryTests.cs`.

**`EventListeners/` fica à parte**, como no projeto real. A sequência acima é a da entrada HTTP; a mensagem é uma segunda porta de entrada, com sua própria sequência sobre a mesma espinha:

```
Listeners/  →  Services/  →  Repositories/
```

As demais pastas mantêm o nome que já têm no projeto: `Entities/`. A única sem contrapartida real é `Suporte/`, que guarda os fixtures.

## Estado

**Implementado — 209 testes passando**: 101 unitários e 108 de integração.

```bash
cd API-Faturamento && dotnet test
```

## Estrutura

```
Faturamento.TestesUnitarios
  Controllers/
    BaseControllerTests.cs             Result → ProblemDetails com code e traceId
  Services/
    InvoiceServiceTests.cs
    InvoicePrintServiceTests.cs        transições da impressão
    ProductReplicationServiceTests.cs
  Entities/
    InvoiceTests.cs                    StartPrinting, Close, Reject, ExpirePrinting
                                       e as derivadas Printing e Editable
    InvoiceItemTests.cs
    ReplicatedProductTests.cs
    ProcessedMessageTests.cs
    OutboxMessageTests.cs
  EventListeners/
    UrnExchangeNameFormatterTests.cs   o nome da exchange sai sem o prefixo urn:message:
    Messages/
      MessageUrnTests.cs               todo contrato tem [MessageUrn] e o valor esperado

Faturamento.TestesIntegracao
  Suporte/
    PostgresFixture.cs                 container PostgreSQL, migrations aplicadas
    RabbitMqFixture.cs                 container RabbitMQ, vhost limpo por execução
    FaturamentoApiFactory.cs           WebApplicationFactory sobre os containers
  Controllers/
    NotasControllerTests.cs            HTTP completo: auth, binding, status, ProblemDetails
  Services/
    InvoicePrintServiceTests.cs        transação e idempotência contra banco real
  Repositories/
    InvoiceRepositoryTests.cs          filtro de situação, paginação, AsSplitQuery
    ReplicatedProductRepositoryTests.cs
    ProcessedMessageRepositoryTests.cs
    OutboxRepositoryTests.cs
    UnitOfWorkTests.cs
  EventListeners/
    Listeners/
      ConsumidoresTests.cs             os quatro consumidores e as definições de fila
    Workers/
      OutboxDispatcherWorkerTests.cs   mapa de tipos completo, sem mensagem presa
      PrintExpirationWorkerTests.cs    tolerância maior que o intervalo de varredura
    Publishers/
      FaturamentoEventPublisherTests.cs
    EventListenerServiceTests.cs       a fila nasce do ConsumerDefinition, sem registro manual
```

## A regra de divisão

**Unitário é o que não toca em I/O.** A entidade `Invoice` concentra o ciclo de vida da impressão e é a melhor relação custo-benefício do serviço: lógica pura, sem mock. Os serviços usam as interfaces de `Faturamento.Domain/Interfaces` como costura.

**`Repositories/` só existe na integração.** Não é omissão: o filtro de situação é um `switch` que vira SQL — só o banco confirma a tradução — e `AsSplitQuery` existe para evitar explosão cartesiana, o que só aparece com dados reais.

**`Controllers/` no unitário tem só o `BaseController`.** As actions são de uma linha (`=> Respond(await service.X(...))`); testá-las com o service mockado afirma que o mock foi chamado. Rota, policy, binding e o contrato de que mutação de item devolve a nota inteira só aparecem com o pipeline HTTP de pé.

**`InvoicePrintServiceTests` aparece nos dois**, e é intencional: as transições de estado da nota são mockáveis; a idempotência, que depende da tabela `mensagens_processadas` gravada na mesma transação, só tem significado contra o PostgreSQL.

## Por que os dois testes de mensageria no unitário

Eles cobrem uma falha que **não aparece em runtime até ser tarde demais**, e que já aconteceu neste projeto:

- **`UrnExchangeNameFormatterTests`** — sem o formatter, o MassTransit nomeia a exchange pelo namespace CLR, e publicador e consumidor com a mesma URN vão parar em exchanges diferentes. Não há erro: as filas ficam saudáveis e nenhuma mensagem trafega. O teste fixa o nome esperado (`emissor:baixar-estoque`) e falha se o formatter for removido.
- **`MessageUrnTests`** — o `[MessageUrn]` lança se o valor incluir o prefixo `urn:message:`, mas a exceção nasce no construtor do atributo, então só surge quando algo reflete sobre o tipo (o `System.Text.Json` serializando no outbox). Não é erro de compilação nem de boot. Um teste que varre o assembly e valida todos os contratos antecipa isso para o build.

O que **não** dá para testar de dentro de um serviço: que a URN é idêntica à do outro lado. Isso exige os dois assemblies. Os testes aqui fixam o valor literal esperado dos dois lados, o que quebra o build de um dos serviços se alguém mudar só um.

## Pacotes

`xunit` · `NSubstitute` · `Shouldly` · `Testcontainers.PostgreSql` · `Testcontainers.RabbitMq` · `Microsoft.AspNetCore.Mvc.Testing`

FluentAssertions não é usada: a partir da v8 exige licença comercial para uso não open-source.

## Execução

```bash
cd API-Faturamento
dotnet test                                  # os dois projetos
dotnet test Testes/TestesUnitarios           # só unitários, sem Docker
```

Os testes de integração exigem Docker em execução; os unitários não.
