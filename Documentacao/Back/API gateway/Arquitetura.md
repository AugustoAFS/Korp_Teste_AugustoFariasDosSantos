# Arquitetura — API Gateway

Ponto único de entrada · .NET 10 · PostgreSQL

## Estrutura — MVC, projeto único

Sem Clean Architecture: o gateway só autentica, autoriza e emite credenciais.

Solution `API-Gateway.slnx` com um projeto.

```
API-Gateway/
  API-Gateway.slnx
  Gateway/
    Config/
      AuthConfig.cs
      CorsConfig.cs
      DataProtectionConfig.cs
      HealthCheckConfig.cs
      OpenApiConfig.cs
      RateLimitConfig.cs
      SecuritySchemeTransformer.cs
      ValidationConfig.cs
      VersioningConfig.cs
    Controllers/
      AuthController.cs
      BaseController.cs
      UsersController.cs
    Data/
      Configurations/
      Migrations/
      AdminSeeder.cs
      DatabaseInitializer.cs
      DefaultRoles.cs
      GatewayDbContext.cs
      RoleSeeder.cs
    DependencyInjection/
      ConventionRegistration.cs 
      DependencyInjectionService.cs
    Dtos/
      Request/
      Response/
      Result.cs
      ResultOfT.cs
    Exceptions/
      Error.cs
      Errors.cs
    Middleware/
      AntiforgeryMiddleware.cs
      ExceptionMiddleware.cs
      ProblemResponse.cs
    Models/
      Enums/
      AuditableEntity.cs
      Role.cs
      User.cs
      UserRole.cs
    Repositories/
      Interfaces/
    Security/
      Interfaces/
      Argon2idHasher.cs
      SessionValidator.cs
      TokenService.cs
    Services/
      Interfaces/
      AuthService.cs
      UserService.cs
    Properties/
    Dockerfile
    Program.cs
```

## Responsabilidades

- Cadastro e consulta de usuário, e autenticação local (Argon2id com pepper)
- Emissão do cookie de sessão e do token antifalsificação
- Emissão do JWT interno assinado, para os serviços downstream validarem
- Administração de perfis de usuário
- Rate limit e CORS

- Roteamento reverso (YARP) para os serviços downstream, trocando cookie por JWT interno
- Circuit breaker (Polly) por cluster downstream
- Entrega dos estáticos do front, na mesma origem

O front fala **somente** com o gateway, sempre por cookie. O YARP recebe a requisição já
autenticada, emite o JWT interno com `ITokenService`, injeta em `Authorization: Bearer` e
remove o header `Cookie` antes de encaminhar — o downstream nunca vê o cookie e o front nunca
vê o token. `GET /api/v1/auth/token` continua existindo, mas deixa de ser necessário para o
front.


## Endpoints

```
POST   /api/v1/users            cadastro; anônimo sai como Funcionario, Administrador escolhe os perfis
GET    /api/v1/users/{id}       dados do usuário; o próprio, ou qualquer um se for Administrador
PUT    /api/v1/users/{id}/roles troca de perfis (somente Administrador)
POST   /api/v1/auth/login       204 + Set-Cookie
POST   /api/v1/auth/logout      204
GET    /api/v1/auth/me          { name, email, roles } da sessão corrente
GET    /api/v1/auth/token       { token, expiresIn } — JWT interno
GET    /health/live             liveness, sem checar dependência
GET    /health/ready            readiness, com checagem do banco
```