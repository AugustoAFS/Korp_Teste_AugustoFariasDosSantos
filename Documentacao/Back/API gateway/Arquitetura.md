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
      AuthConfig.cs                cookie, antiforgery e fallback policy
      CorsConfig.cs                origens permitidas, lidas da configuração
      DataProtectionConfig.cs      chaves persistidas no banco
      HealthCheckConfig.cs         /health/live e /health/ready
      OpenApiConfig.cs             OpenAPI + Scalar
      RateLimitConfig.cs           limite global e política de credenciais
      SecuritySchemeTransformer.cs cookie e X-XSRF-TOKEN no OpenAPI
      ValidationConfig.cs          contrato de erro de validação
      VersioningConfig.cs
    Controllers/
      AuthController.cs
      BaseController.cs
      UsersController.cs
    Data/
      Configurations/              mapeamentos Fluent API
      Migrations/
      AdminSeeder.cs               administrador inicial, vindo da configuração
      DatabaseInitializer.cs       espera o servidor, cria o banco, migra
      DefaultRoles.cs              fonte única dos perfis padrão
      GatewayDbContext.cs
      RoleSeeder.cs                conferência idempotente dos perfis
    DependencyInjection/
      ConventionRegistration.cs    Services e Repositories por convenção
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

O front fala **somente** com o gateway, sempre por cookie. O YARP recebe a requisição já
autenticada, emite o JWT interno com `ITokenService`, injeta em `Authorization: Bearer` e
remove o header `Cookie` antes de encaminhar — o downstream nunca vê o cookie e o front nunca
vê o token. `GET /api/v1/auth/token` continua existindo, mas deixa de ser necessário para o
front.

O circuit breaker (Polly) **não faz parte** desta entrega.

| Rota no gateway | Destino |
|---|---|
| `/api/v1/produtos` e `/api/v1/produtos/{**resto}` | cluster `estoque` |

O endereço do cluster vem de `ReverseProxy:Clusters:estoque:Destinations:primary:Address` —
`http://localhost:5247/` em desenvolvimento, `http://estoque:8080/` no compose. Ambas as rotas
exigem autenticação pela `AuthorizationPolicy: default`, e requisição insegura continua
passando pelo antiforgery do gateway.

O gateway **não publica nem consome eventos**: não participa da saga de baixa de estoque,
não tem outbox e não depende do RabbitMQ.

## Convenções

- Um tipo por arquivo; interface e implementação em arquivos separados
- Interfaces ficam em subpasta `Interfaces` e repetem o nome da implementação com `I` na frente
- `Services` e `Repositories` são registrados por convenção; uma classe sem a interface correspondente derruba o boot
- `Security` é registrado explicitamente: são singletons que recebem segredos já validados
- Configuração obrigatória, sem fallback: chave ausente ou inválida derruba o boot nomeando a chave

## Configuração

| Chave | Origem esperada |
|---|---|
| `ConnectionStrings:GatewayDb` | `appsettings.json` em desenvolvimento, variável de ambiente em container |
| `Cors:Origins` | `appsettings.json` |
| `Security:Pepper` | `appsettings.json` — chave de desenvolvimento, versionada de propósito |
| `Security:JwtKey` | `appsettings.json` — **precisa ser igual à do Estoque** |
| `Seed:Admin:Name` · `Email` · `Password` | `appsettings.json` — administrador inicial |

O banco é criado e migrado no boot, depois de o servidor responder. Ausência do servidor
gera até dez tentativas espaçadas de três segundos antes de falhar nomeando a chave.

Não há user secrets: toda a configuração está no `appsettings.json` para que o projeto rode
com um `git clone` seguido de `dotnet run`. `Security:JwtKey` e `Security:Pepper` são chaves
de desenvolvimento, versionadas de propósito pelo mesmo motivo do administrador inicial. Em
ambiente real as duas devem vir de variável de ambiente — trocar o pepper invalida todas as
senhas já gravadas, e trocar a `JwtKey` exige trocar nos dois serviços ao mesmo tempo.

## Administrador inicial

Para que a aplicação seja avaliável sem depender de cadastro manual, o boot cria um
administrador a partir da seção `Seed:Admin`:

| Campo | Valor |
|---|---|
| E-mail | `admin@admin.com` |
| Senha | `Admin123!` |
| Perfil | Administrador |

A operação é idempotente: se o usuário já existir, nada é criado, e o perfil Administrador é
devolvido caso tenha sido removido. Se a seção `Seed:Admin` for omitida, nenhum usuário é
semeado; se estiver presente e incompleta, o boot falha nomeando a chave.

Essa credencial está versionada de propósito, para o avaliador conseguir entrar. Em qualquer
ambiente real ela deve ser sobrescrita por variável de ambiente ou a seção removida — o boot
registra um aviso sempre que cria o usuário.

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

`POST /api/v1/users` é um endpoint só, com o comportamento decidido pela autorização de quem
chama: requisição anônima ignora o campo `Roles` e o usuário nasce `Funcionario`; requisição
autenticada de um `Administrador` respeita os perfis informados.

Toda rota exige autenticação por padrão (fallback policy); o acesso anônimo é explícito.

A proteção contra CSRF é feita em duas camadas. A primeira é o cookie de sessão com
`SameSite=Strict`, que impede o navegador de anexá-lo em requisições originadas de outro
site — o que só funciona enquanto front e API forem o mesmo site registrável. A segunda é o
token antifalsificação: requisição insegura de usuário autenticado exige o header
`X-XSRF-TOKEN` com o valor do cookie `XSRF-TOKEN`, publicado a cada requisição segura fora
de `/health`. O login é isento, por ainda não haver sessão.

A sessão é revalidada a cada requisição: usuário inativo, excluído, com senha alterada
depois da emissão do cookie ou com perfis diferentes dos que estão no cookie perde o acesso
imediatamente.

## JWT interno

Contrato que os serviços downstream devem validar:

| Campo | Valor |
|---|---|
| Algoritmo | HS256, chave `Security:JwtKey` |
| `iss` | `emissor-gateway` |
| `aud` | `emissor-servicos` |
| Validade | 120 segundos |
| Claims | `sub` (id do usuário), `name`, `email`, `jti`, `role` (uma por perfil) |

## Tratamento de erros

Todo erro sai como `ProblemDetails` (RFC 7807) com as extensões `code` e `traceId`.

| Situação | Retorno | `code` |
|---|---|---|
| Payload inválido | 400 + `errors` por campo | `validation_error` |
| Credencial inválida | 401 (mensagem genérica, sem revelar se o e-mail existe) | `invalid_credentials` |
| Sessão ausente, expirada ou revogada | 401 | `invalid_session` |
| Usuário bloqueado (5 tentativas → 15 min) | 403 | `user_locked` |
| Usuário inativo | 403 | `user_inactive` |
| Sem perfil para a rota | 403 | `forbidden` |
| Usuário inexistente | 404 | `user_not_found` |
| E-mail já cadastrado | 409 | `email_in_use` |
| Perfil inexistente | 422 | `role_not_found` |
| Antiforgery ausente / inválido | 400 | `invalid_antiforgery_token` |
| Limite de requisições | 429 + `Retry-After` | `too_many_requests` |
| Não tratado | middleware → 500 | `internal_error` |

O bloqueio só é revelado a quem acerta a senha: senha errada devolve sempre
`invalid_credentials`, para não transformar o lockout em oráculo de contas existentes.
