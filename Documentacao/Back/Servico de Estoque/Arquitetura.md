# Arquitetura — Serviço de Estoque

Clean Architecture · .NET 10 · SQL Server

## Projetos

```
Estoque.Api
  Configurations/
    AuthConfig.cs
    CorsConfig.cs
    HealthCheckConfig.cs
    OpenApiConfig.cs
    RateLimitConfig.cs
    ValidationConfig.cs
    VersioningConfig.cs
  Controllers/
    BaseController.cs
    ProdutosController.cs
  Data/
    DatabaseInitializer.cs
  Middlewares/
    ExceptionMiddleware.cs
    ProblemResponse.cs
  Properties/
  Program.cs

Estoque.ApplicationService
  DependencyInjection/
    DependencyInjectionService.cs
  Interfaces/
    IProductService.cs
    IStockDebitService.cs
    IEstoqueEventPublisher.cs
  Services/
    ProductService.cs
    StockDebitService.cs

Estoque.Domain
  Dtos/
    Request/
    Response/
    EventListeners/
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
    Configurations/
  DependencyInjection/
    DependencyInjectionService.cs
  Migrations/
  Repositories/
    ProductRepository.cs · StockMovementRepository.cs
    ProcessedMessageRepository.cs · OutboxRepository.cs · UnitOfWork.cs
  Security/
    CurrentUser.cs

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
