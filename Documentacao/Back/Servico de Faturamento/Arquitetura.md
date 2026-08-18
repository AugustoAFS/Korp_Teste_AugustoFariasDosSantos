# Arquitetura — Serviço de Faturamento

Clean Architecture · .NET 10 · PostgreSQL

## Projetos

```
Faturamento.Api
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
    NotasController.cs
  Data/
    DatabaseInitializer.cs
  Middlewares/
    ExceptionMiddleware.cs
    ProblemResponse.cs
  Program.cs

Faturamento.ApplicationService
  DependencyInjection/
  Interfaces/
    IInvoiceService.cs
    IInvoicePrintService.cs
    IProductReplicationService.cs
    IFaturamentoEventPublisher.cs
  Services/
    InvoiceService.cs
    InvoicePrintService.cs
    ProductReplicationService.cs

Faturamento.Domain
  Dtos/
    Request/                       AddInvoiceItemRequest · UpdateInvoiceItemRequest
                                   InvoiceFilterRequest
    Response/                      InvoiceResponse · InvoiceItemResponse · PagedResult<T>
    EventListeners/                DebitItem · PendingOutboxMessage
    Result.cs · ResultOfT.cs
  Entities/                        Invoice · InvoiceItem · ReplicatedProduct
                                   OutboxMessage · ProcessedMessage · AuditableEntity
  Enums/                           InvoiceStatus
  Exceptions/                      Error · Errors
  Interfaces/
    IInvoiceRepository.cs
    IReplicatedProductRepository.cs
    IProcessedMessageRepository.cs
    IOutboxRepository.cs
    IUnitOfWork.cs
    ICurrentUser.cs

Faturamento.InfraStructure
  Data/
    FaturamentoDbContext.cs
    Configurations/
  DependencyInjection/
  Migrations/
  Repositories/
  Security/
    CurrentUser.cs

Faturamento.EventListeners
  Messages/
    Publicados/                    BaixarEstoqueCommand · ItemBaixaEstoque
    Consumidos/                    EstoqueBaixadoEvent · ItemBaixado
                                   EstoqueRejeitadoEvent
                                   ProdutoCriadoEvent · ProdutoAtualizadoEvent
  Listeners/
    OnEstoqueBaixado.cs
    OnEstoqueRejeitado.cs
    OnProdutoCriado.cs
    OnProdutoAtualizado.cs
  Publishers/
    FaturamentoEventPublisher.cs
  Workers/
    OutboxDispatcherWorker.cs
    PrintExpirationWorker.cs
  UrnExchangeNameFormatter.cs
  EventListenerService.cs
```

Dependências:

```
Api                   → ApplicationService · InfraStructure · EventListeners
ApplicationService    → Domain
InfraStructure        → Domain
EventListeners        → ApplicationService · InfraStructure
```

## Endpoints

Todos exigem sessão. O gateway troca o cookie pelo JWT interno; o serviço nunca vê o cookie.

```
GET    /api/v1/notas?page=1&size=20&status=       → PagedResult<InvoiceResponse>
GET    /api/v1/notas/{id}                         → InvoiceResponse
POST   /api/v1/notas                              → 201 InvoiceResponse
DELETE /api/v1/notas/{id}                         → 204
POST   /api/v1/notas/{id}/itens                   → 201 InvoiceResponse
PUT    /api/v1/notas/{id}/itens/{itemId}          → 200 InvoiceResponse
DELETE /api/v1/notas/{id}/itens/{itemId}          → 204
POST   /api/v1/notas/{id}/impressao               → 202 InvoiceResponse
```

Erros seguem RFC 7807 com `code` e `traceId`:

```
invoice_not_found          404
invoice_item_not_found     404
invoice_not_editable       409
invoice_already_printing   409
invoice_already_closed     409
invoice_item_duplicated    409
invoice_empty              422
product_not_found          422   produto ainda não replicado
product_inactive           422
```