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

```bash
cd API-Gateway && dotnet test
OU
dotnet test //somente se ja estiver com o projeto aberto na IDE
```

## Estrutura

```
Gateway.TestesUnitarios
  Controllers/
    BaseControllerTests.cs
  Services/
    AuthServiceTests.cs
    UserServiceTests.cs
  Security/
    Argon2idHasherTests.cs
    TokenServiceTests.cs
    SessionValidatorTests.cs
  Middleware/
    ProblemResponseTests.cs
  Models/
    UserTests.cs
    RoleTests.cs

Gateway.TestesIntegracao
  Suporte/
    PostgresFixture.cs
    GatewayApiFactory.cs
    DownstreamStub.cs
  Controllers/
    AuthControllerTests.cs
    UsersControllerTests.cs
  Repositories/
    UserRepositoryTests.cs
    RoleRepositoryTests.cs
  Middleware/
    AntiforgeryMiddlewareTests.cs
    ExceptionMiddlewareTests.cs
  Config/
    ResilienceConfigTests.cs
    ReverseProxyConfigTests.cs
  Data/
    RoleSeederTests.cs
    AdminSeederTests.cs
```