# Testes — Serviço de Estoque

xUnit · NSubstitute · Testcontainers (SQL Server + RabbitMQ)

## Localização

Os dois projetos ficam dentro do próprio serviço e entram em `API-Estoque.slnx`. Não há projeto de teste na raiz do repositório: um projeto que referenciasse os três serviços acoplaria os microsserviços em tempo de build.

```
API-Estoque/
  Testes/
    TestesUnitarios/     Estoque.TestesUnitarios.csproj
    TestesIntegracao/    Estoque.TestesIntegracao.csproj
```

## Convenção

A organização segue a **sequência de execução**, não as camadas da Clean Architecture:

```
Controllers/  →  Services/  →  Repositories/
```

O nome do arquivo espelha o do projeto com o sufixo `Tests`. A camada de origem é descartada — `Estoque.InfraStructure/Repositories/ProductRepository.cs` vira `Repositories/ProductRepositoryTests.cs`.

**`EventListeners/` fica à parte**, como no projeto real. A sequência acima é a da entrada HTTP; a mensagem é uma segunda porta de entrada, com sua própria sequência sobre a mesma espinha:

```
Listeners/  →  Services/  →  Repositories/
```

As demais pastas mantêm o nome que já têm no projeto: `Entities/`. A única sem contrapartida real é `Suporte/`, que guarda os fixtures.

## Estado

**Implementado — 169 testes passando**: 72 unitários e 97 de integração.

```bash
cd API-Estoque && dotnet test
```

## Estrutura

```
Estoque.TestesUnitarios
  Controllers/
    BaseControllerTests.cs             Result → ProblemDetails com code e traceId
  Services/
    ProductServiceTests.cs
    StockDebitServiceTests.cs          decisão de tudo-ou-nada, replay e skip
  Entities/
    ProductTests.cs
    StockMovementTests.cs
    ProcessedMessageTests.cs
    OutboxMessageTests.cs
  EventListeners/
    UrnExchangeNameFormatterTests.cs   o nome da exchange sai sem o prefixo urn:message:
    Messages/
      MessageUrnTests.cs               todo contrato tem [MessageUrn] e o valor esperado

Estoque.TestesIntegracao
  Suporte/
    SqlServerFixture.cs                container SQL Server, migrations aplicadas
    RabbitMqFixture.cs                 container RabbitMQ
    EstoqueApiFactory.cs               WebApplicationFactory sobre os containers, emite JWT por perfil
    BancoCollection.cs                 sobe os containers uma vez para toda a suíte
  Controllers/
    ProdutosControllerTests.cs         HTTP completo: auth, binding, status, ProblemDetails
  Services/
    StockDebitServiceTests.cs          savepoint e transação contra banco real
  Repositories/
    ProductRepositoryTests.cs          UPDATE condicional, constraint, concorrência
    ProcessedMessageRepositoryTests.cs
    StockMovementRepositoryTests.cs
    OutboxRepositoryTests.cs
    UnitOfWorkTests.cs
  EventListeners/
    Listeners/
      OnBaixarEstoqueTests.cs          consumidor real: tudo-ou-nada, replay, concorrência
    Workers/
      OutboxDispatcherWorkerTests.cs   publica o pendente e marca como publicado
    Publishers/
      EstoqueEventPublisherTests.cs
    EventListenerServiceTests.cs       a fila nasce do ConsumerDefinition, sem registro manual
```

## A regra de divisão

**Unitário é o que não toca em I/O.** Entidades e serviços com os repositórios mockados pelas interfaces de `Estoque.Domain/Interfaces` — as costuras já existem por causa da Clean Architecture.

**`Repositories/` só existe na integração.** Não é omissão: o repositório mais importante do serviço usa `ExecuteUpdateAsync`, que **não roda** no provider InMemory, e depende da constraint `ck_products_balance`, que só existe no banco. Um teste com mock ali passa verde sem provar nada.

**`Controllers/` no unitário tem só o `BaseController`.** As actions são de uma linha (`=> Respond(await service.X(...))`); testá-las com o service mockado afirma que o mock foi chamado. Rota, policy, binding e status code só aparecem com o pipeline HTTP de pé, por isso `ProdutosControllerTests` fica na integração.

**`StockDebitServiceTests` aparece nos dois**, e é intencional: a decisão (rejeitar tudo quando falta saldo, reemitir o desfecho numa duplicata, abortar quando perde a corrida) é mockável; o savepoint que desfaz baixas parciais preservando o marcador de idempotência é transacional e só existe contra o SQL Server.

## Por que os dois testes de mensageria no unitário

Eles cobrem uma falha que **não aparece em runtime até ser tarde demais**, e que já aconteceu neste projeto:

- **`UrnExchangeNameFormatterTests`** — sem o formatter, o MassTransit nomeia a exchange pelo namespace CLR, e publicador e consumidor com a mesma URN vão parar em exchanges diferentes. Não há erro: as filas ficam saudáveis e nenhuma mensagem trafega. O teste fixa o nome esperado (`emissor:produto-criado`) e falha se o formatter for removido.
- **`MessageUrnTests`** — o `[MessageUrn]` lança se o valor incluir o prefixo `urn:message:`, mas a exceção nasce no construtor do atributo, então só surge quando algo reflete sobre o tipo (o `System.Text.Json` serializando no outbox). Não é erro de compilação nem de boot. Um teste que varre o assembly e valida todos os contratos antecipa isso para o build.

O que **não** dá para testar de dentro de um serviço: que a URN é idêntica à do outro lado. Isso exige os dois assemblies. Os testes aqui fixam o valor literal esperado dos dois lados, o que quebra o build de um dos serviços se alguém mudar só um.

## Pacotes

`xunit` · `NSubstitute` · `Shouldly` · `Testcontainers.MsSql` · `Testcontainers.RabbitMq` · `Microsoft.AspNetCore.Mvc.Testing`

FluentAssertions não é usada: a partir da v8 exige licença comercial para uso não open-source.

## Execução

```bash
cd API-Estoque
dotnet test                                  # os dois projetos
dotnet test Testes/TestesUnitarios           # só unitários, sem Docker
```

Os testes de integração exigem Docker em execução; os unitários não.
