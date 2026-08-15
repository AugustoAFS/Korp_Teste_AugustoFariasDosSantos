# Arquitetura — Serviço de Estoque

Clean Architecture · .NET 10 · SQL Server

## Projetos

```
Estoque.Api
  Configurations/
    AuthConfig.cs                JWT Bearer do gateway, fallback policy, política de escrita
    CorsConfig.cs                origens permitidas, lidas da configuração
    HealthCheckConfig.cs         /health/live e /health/ready
    OpenApiConfig.cs             OpenAPI + Scalar
    RateLimitConfig.cs           limite global por sub do JWT ou IP
    ValidationConfig.cs          contrato de erro de validação
    VersioningConfig.cs
  Controllers/
    BaseController.cs
    ProdutosController.cs
  Data/
    DatabaseInitializer.cs       espera o servidor, cria o banco, migra
  Middlewares/
    ExceptionMiddleware.cs
    ProblemResponse.cs
  Properties/
  Program.cs

Estoque.ApplicationService
  DependencyInjection/
    DependencyInjectionService.cs  varre Services do próprio assembly
  Interfaces/
    IProductService.cs
    IStockDebitService.cs
    IEstoqueEventPublisher.cs
  Services/
    ProductService.cs
    StockDebitService.cs

Estoque.Domain
  Dtos/
    Request/                     CreateProductRequest · UpdateProductRequest · ProductFilterRequest
    Response/                    ProductResponse · PagedResult<T>
    EventListeners/              DebitItem · UpdatedBalance · DebitResult · PendingOutboxMessage
    Result.cs · ResultOfT.cs
  Entities/
    AuditableEntity.cs · Product.cs · StockMovement.cs
    OutboxMessage.cs · ProcessedMessage.cs
  Enums/
    MovementType.cs
  Exceptions/
    Error.cs · Errors.cs
  Interfaces/
    IProductRepository.cs
    IStockMovementRepository.cs
    IProcessedMessageRepository.cs
    IOutboxRepository.cs
    IUnitOfWork.cs
    ICurrentUser.cs

Estoque.InfraStructure
  Data/
    EstoqueDbContext.cs
    Configurations/              mapeamentos Fluent API
  DependencyInjection/
    DependencyInjectionService.cs  DbContext + varredura de Repositories e Security
  Migrations/
  Repositories/
    ProductRepository.cs · StockMovementRepository.cs
    ProcessedMessageRepository.cs · OutboxRepository.cs · UnitOfWork.cs
  Security/
    CurrentUser.cs               adaptador da claim sub para ICurrentUser

Estoque.EventListeners
  Messages/
    Publicados/                  EstoqueBaixadoEvent · ItemBaixado · EstoqueRejeitadoEvent
                                 ProdutoCriadoEvent · ProdutoAtualizadoEvent
    Consumidos/                  BaixarEstoqueCommand · ItemBaixaEstoque
  Listeners/
    OnBaixarEstoque.cs
  Publishers/
    EstoqueEventPublisher.cs
  Workers/
    OutboxDispatcherWorker.cs
  EventListenerService.cs
```
Api                   → ApplicationService · InfraStructure · EventListeners
ApplicationService    → Domain
InfraStructure        → Domain
EventListeners        → ApplicationService · InfraStructure

Não há registro de dependência na Api: cada camada expõe o seu próprio
`DependencyInjectionService`, que varre o próprio assembly e registra toda classe que exponha
a interface `I{Nome}`. `Program.cs` apenas encadeia `AddInfraStructure` ·
`AddApplicationService` · `AddEventListeners`.

## Convenções

- Um tipo por arquivo; interface e implementação em arquivos separados
- Interfaces de repositório e portas ficam em `Estoque.Domain/Interfaces` e repetem o nome da
  implementação com `I` na frente; a implementação vive na InfraStructure
- `Services` e `Repositories` são registrados por convenção; uma classe sem a interface
  correspondente derruba o boot
- Configuração obrigatória, sem fallback: chave ausente ou inválida derruba o boot nomeando a chave
- Serviços devolvem `Result`/`Result<T>` com o catálogo `Errors`, nunca exception para
  fluxo previsto

## Configuração

| Chave | Origem esperada |
|---|---|
| `ConnectionStrings:EstoqueDb` | `appsettings.json` em desenvolvimento, variável de ambiente em container |
| `ConnectionStrings:RabbitMq` | `appsettings.json` em desenvolvimento, variável de ambiente em container |
| `Cors:Origins` | `appsettings.json` |
| `Security:JwtKey` | `appsettings.json` — **precisa ser a mesma do gateway** |

O banco é criado e migrado no boot, depois de o servidor responder — basta subir a
infraestrutura e rodar o projeto, sem passo manual. Ausência do servidor gera até dez
tentativas espaçadas de três segundos antes de falhar nomeando a chave; banco já em dia não
faz nada.

## Endpoints

```
GET    /api/v1/produtos?page=1&size=20&search=      PagedResult<ProductResponse>
GET    /api/v1/produtos/{id}                         ProductResponse
POST   /api/v1/produtos                              201 ProductResponse
PUT    /api/v1/produtos/{id}                         200 ProductResponse
DELETE /api/v1/produtos/{id}                         204 exclusão lógica
GET    /health/live                                  liveness, sem checar dependência
GET    /health/ready                                 readiness, com banco e broker
```

O front não chama este serviço diretamente: ele fala com o gateway em `:5000` usando o cookie
de sessão, e o YARP do gateway emite o JWT interno e o encaminha como `Authorization: Bearer`.
O Estoque valida esse token por conta própria — quem alcançar a porta 5247 sem token recebe
401, então o proxy não é a única linha de defesa. Contrato validado: HS256 com
`Security:JwtKey`, `iss` `emissor-gateway`, `aud` `emissor-servicos`,
`ClockSkew` zerado (a validade é de 120 segundos — a tolerância padrão de 5 minutos anularia
a decisão do gateway), `MapInboundClaims = false`, `NameClaimType` `name` e `RoleClaimType`
`role`.

Leitura exige apenas autenticação. Escrita exige a política `escrita-de-produto`
(`Administrador` ou `Gerente`). Não há antiforgery: o serviço é consumido por header
`Authorization`, não por cookie.

`POST` recebe `codigo`, `descricao` e `saldo` — o saldo é o inicial e gera um movimento de
Entrada. `PUT` recebe `codigo`, `descricao` e `ativo`: saldo não se altera por edição de
cadastro, só pela baixa da nota.

## Tratamento de erros

Todo erro sai como `ProblemDetails` (RFC 7807) com as extensões `code` e `traceId`, igual ao
gateway.

| Situação | Retorno | `code` |
|---|---|---|
| Payload inválido | 400 + `errors` por campo | `validation_error` |
| Token ausente, expirado ou inválido | 401 | `invalid_session` |
| Sem perfil para escrita | 403 | `forbidden` |
| Produto inexistente | 404 | `product_not_found` |
| Código já cadastrado | 409 | `product_code_in_use` |
| Produto inativo | 422 | `product_inactive` |
| Exclusão de produto com saldo | 422 | `product_with_balance` |
| Saldo insuficiente | 422 | `insufficient_balance` |
| Limite de requisições | 429 + `Retry-After` | `too_many_requests` |
| Não tratado | middleware → 500 | `internal_error` |

## Mensageria

`AddConsumers` varre o assembly e `ConfigureEndpoints` cria as filas a partir da `Definition`
aninhada em cada `On*` — listener novo não exige alteração no registro.

`OnBaixarEstoque` roda com `ConcurrentMessageLimit = 5` de propósito: serializar em 1
esconderia a disputa de saldo em vez de tratá-la. A corretude vem do `UPDATE` condicional e do
`CHECK (saldo >= 0)`.

`OutboxDispatcherWorker` recebe `IBus`, não `IPublishEndpoint`: `IPublishEndpoint` é scoped no
MassTransit e um `BackgroundService` é singleton — injetá-lo derruba o boot com
`ValidateScopes` ligado.

O valor de `[MessageUrn]` **não** inclui o prefixo `urn:message:`, que o MassTransit acrescenta
sozinho e recusa se vier duplicado. `[MessageUrn("emissor:baixar-estoque")]` produz o URN
`urn:message:emissor:baixar-estoque` no envelope. O sufixo precisa ser idêntico nos dois
serviços.

| Contrato | URN efetivo |
|---|---|
| `BaixarEstoqueCommand` (consumido) | `urn:message:emissor:baixar-estoque` |
| `EstoqueBaixadoEvent` | `urn:message:emissor:estoque-baixado` |
| `EstoqueRejeitadoEvent` | `urn:message:emissor:estoque-rejeitado` |
| `ProdutoCriadoEvent` | `urn:message:emissor:produto-criado` |
| `ProdutoAtualizadoEvent` | `urn:message:emissor:produto-atualizado` |
