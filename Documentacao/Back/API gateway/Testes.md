# Testes — API Gateway

xUnit · NSubstitute · Testcontainers (PostgreSQL)

## Localização

Os dois projetos ficam dentro do próprio serviço e entram em `API-Gateway.slnx`. Não há projeto de teste na raiz do repositório: um projeto que referenciasse os três serviços acoplaria os microsserviços em tempo de build.

```
API-Gateway/
  Testes/
    TestesUnitarios/     Gateway.TestesUnitarios.csproj
    TestesIntegracao/    Gateway.TestesIntegracao.csproj
```

## Convenção

A organização segue a **sequência de execução**:

```
Controllers/  →  Services/  →  Repositories/
```

O nome do arquivo espelha o do projeto com o sufixo `Tests`. As pastas fora dessa sequência mantêm o nome que já têm no projeto: `Security/`, `Middleware/`, `Models/`, `Config/`, `Data/`. A única sem contrapartida real é `Suporte/`, que guarda os fixtures.

O Gateway não usa Clean Architecture — ele não tem domínio próprio, é autenticação e roteamento — então aqui não havia camada para descartar: as pastas já são as de `Gateway/` direto.

## Estado

**Implementado — 183 testes passando**: 99 unitários e 84 de integração.

```bash
cd API-Gateway && dotnet test
```

## Estrutura

```
Gateway.TestesUnitarios
  Controllers/
    BaseControllerTests.cs             Result → ProblemDetails com code e traceId
  Services/
    AuthServiceTests.cs                login, credencial inválida, usuário inativo
    UserServiceTests.cs                criação, atribuição de papéis, filtro paginado
  Security/
    Argon2idHasherTests.cs             hash e verificação, efeito do pepper
    TokenServiceTests.cs               claims, expiração e assinatura do JWT interno
    SessionValidatorTests.cs
  Middleware/
    ProblemResponseTests.cs
  Models/
    UserTests.cs
    RoleTests.cs

Gateway.TestesIntegracao
  Suporte/
    PostgresFixture.cs                 container PostgreSQL, migrations aplicadas
    GatewayApiFactory.cs               WebApplicationFactory sobre o container
    DownstreamStub.cs                  serviço falso para exercitar proxy e breaker
  Controllers/
    AuthControllerTests.cs             login → cookie → rota protegida → logout
    UsersControllerTests.cs            visibilidade por papel, 403 sem permissão
  Repositories/
    UserRepositoryTests.cs             busca ILIKE, paginação, índice parcial de e-mail
    RoleRepositoryTests.cs
  Middleware/
    AntiforgeryMiddlewareTests.cs      mutação sem token é recusada, com token passa
    ExceptionMiddlewareTests.cs        nenhum erro vaza stack trace
  Config/
    ResilienceConfigTests.cs           downstream falhando abre o circuito
    ReverseProxyConfigTests.cs         breaker aberto vira 503 service_unavailable
  Data/
    RoleSeederTests.cs
    AdminSeederTests.cs
```

## A regra de divisão

**Unitário é o que não toca em I/O.** `Argon2idHasher` e `TokenService` são os melhores candidatos do serviço: entrada e saída determinísticas, nenhuma dependência externa. Os serviços usam as interfaces de `Repositories/Interfaces` como costura para mock.

**`Repositories/` só existe na integração.** Não é omissão: a busca de usuários usa `ILIKE` e um índice parcial, semântica exclusiva do PostgreSQL.

**`Controllers/` no unitário tem só o `BaseController`.** As actions são de uma linha (`=> Respond(await service.X(...))`); o que vale verificar — cookie, antiforgery, policy, status code — só aparece com o pipeline HTTP de pé.

**O contrato do JWT interno fica no unitário.** `TokenServiceTests` valida o token emitido contra a mesma `Security:JwtKey` que os serviços downstream usam, incluindo a recusa quando a chave difere. Isso não exige infraestrutura — é assinatura, e assinatura é matemática.

**Não existe 404 para rota desconhecida.** Toda rota fora das declaradas cai no catch-all do cluster `notaflow`; com o front fora do ar, a resposta é 503 `service_unavailable`. `ExceptionMiddlewareTests` afirma esse comportamento em vez de um 404 que o gateway nunca devolve.

O `DownstreamStub` existe porque o circuit breaker só se comprova com um downstream que realmente falha: um endpoint controlável que devolve erro sob demanda, permitindo verificar que o circuito abre após o limiar e que o YARP passa a responder **503 com `code: service_unavailable`** em vez do 502 sem corpo.

## Nomenclatura

Este serviço é escrito em inglês — tipos, membros, pastas. Classes e métodos de teste seguem o inglês, acompanhando o código sob teste.

## Pacotes

`xunit` · `NSubstitute` · `Shouldly` · `Testcontainers.PostgreSql` · `Microsoft.AspNetCore.Mvc.Testing`

Não há Testcontainers de RabbitMQ aqui: o Gateway não publica nem consome mensagem.

FluentAssertions não é usada: a partir da v8 exige licença comercial para uso não open-source.

## Execução

```bash
cd API-Gateway
dotnet test                                  # os dois projetos
dotnet test Testes/TestesUnitarios           # só unitários, sem Docker
```

Os testes de integração exigem Docker em execução; os unitários não.
