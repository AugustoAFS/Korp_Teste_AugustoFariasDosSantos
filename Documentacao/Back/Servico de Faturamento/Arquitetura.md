# Arquitetura — Serviço de Faturamento

Clean Architecture · .NET 10 · PostgreSQL

## Projetos

```
Faturamento.Api
  Configurations/
    AuthConfig.cs                  JWT interno emitido pelo gateway
    CorsConfig.cs                  origens do front
    HealthCheckConfig.cs           /health/live · /health/ready
    OpenApiConfig.cs               OpenAPI + Scalar
    RateLimitConfig.cs             limitador global por usuário
    ValidationConfig.cs            ModelState → ProblemDetails
    VersioningConfig.cs            Asp.Versioning — versão na URL
  Controllers/
    BaseController.cs              Result/Result<T> → IActionResult
    NotasController.cs
  Data/
    DatabaseInitializer.cs         espera o banco e aplica migrations no boot
  Middlewares/
    ExceptionMiddleware.cs
    ProblemResponse.cs
  Program.cs

Faturamento.ApplicationService
  DependencyInjection/             AddApplicationService() — varredura por convenção
  Interfaces/
    IInvoiceService.cs
    IInvoicePrintService.cs
    IProductReplicationService.cs
    IFaturamentoEventPublisher.cs
  Services/
    InvoiceService.cs              CRUD da nota e dos itens
    InvoicePrintService.cs         ciclo de impressão + saga
    ProductReplicationService.cs   replica o catálogo do estoque

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
    Configurations/                um IEntityTypeConfiguration por tabela
  DependencyInjection/             AddInfraStructure() — varredura por convenção
  Migrations/
  Repositories/
  Security/
    CurrentUser.cs                 lê sub · name · role do JWT

Faturamento.EventListeners         toda a mensageria
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
    FaturamentoEventPublisher.cs   implementa IFaturamentoEventPublisher
  Workers/
    OutboxDispatcherWorker.cs      publica o outbox a cada 2s, lotes de 50
    PrintExpirationWorker.cs       marca impressões sem resposta há 60s
  UrnExchangeNameFormatter.cs      exchange do RabbitMQ = MessageUrn
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

## Visibilidade

`GET /notas` filtra por `issued_by_user_id`. Os perfis `Administrador` e `Gerente`
enxergam todas as notas; os demais só as próprias. Uma nota de outro usuário
responde 404 — nunca 403 — para não revelar a existência do registro.

## Ciclo de impressão

```
POST /notas/{id}/impressao
  ├─ nota fechada          → 409 invoice_already_closed
  ├─ nota imprimindo       → 409 invoice_already_printing
  ├─ nota sem itens        → 422 invoice_empty
  ├─ processing_id nulo    → StartPrinting   (UPDATE condicional, novo Guid)
  └─ processing_id gravado → RestartPrinting (UPDATE condicional, MESMO Guid)
        ↓ mesma transação
  outbox ← BaixarEstoqueCommand              → 202 Accepted
```

Os dois UPDATE são condicionais e servem de trava contra impressão concorrente:
`StartPrinting` só afeta linha com `status = Open AND processing_id IS NULL`;
`RestartPrinting` só afeta linha com `processing_id = @id AND last_error IS NOT NULL`.
Zero linhas afetadas significa que outra requisição chegou primeiro → 409.

Retorno do estoque:

```
EstoqueBaixadoEvent    → Close()   status = Closed, closed_at, limpa processing_id
EstoqueRejeitadoEvent  → Reject()  limpa processing_id, grava last_error
```

Ambos passam por `processed_messages` (dedup por `MessageId`) e só se aplicam se a
nota ainda estiver no mesmo `processing_id` — uma resposta atrasada de um
processamento antigo é descartada.

## Expiração de impressão

`PrintExpirationWorker` roda a cada 15s e marca `last_error` nas notas que estão
imprimindo há mais de 60s. **O `processing_id` é preservado de propósito.** Isso
faz o sistema convergir em dois cenários:

- O estoque estava fora e o comando ficou na fila. Quando ele volta, consome,
  publica o resultado, e a nota fecha sozinha — o `processing_id` ainda casa.
- O usuário manda imprimir de novo antes disso. `RestartPrinting` republica com a
  **mesma** chave. O estoque não debita de novo — mas também não engole a
  mensagem: ele **reemite o resultado que já havia decidido**, gravado em
  `processed_messages.outcome_payload`. Por isso a nota fecha (ou é rejeitada)
  mesmo que o evento original tenha se perdido de vez no broker.

## Replicação do catálogo

`OnProdutoCriado` e `OnProdutoAtualizado` alimentam `replicated_products`, que é o
que a inclusão de item consulta. O upsert só grava se o evento for mais novo que a
linha (`updated_at <= @occurredAt`), então eventos fora de ordem não regridem o
catálogo. Produto ainda não replicado responde 422 `product_not_found`.

## Mensageria

Contratos em `Messages/`, cada um com `[MessageUrn("emissor:...")]`. A URN é o
contrato entre os serviços e também define a exchange do RabbitMQ, via
`UrnExchangeNameFormatter` registrado em `bus.MessageTopology`. Sem esse
formatter o MassTransit nomearia a exchange pelo namespace CLR e publicador e
consumidor jamais se encontrariam.

```
publica   emissor:baixar-estoque
consome   emissor:estoque-baixado      → faturamento.on-estoque-baixado
          emissor:estoque-rejeitado    → faturamento.on-estoque-rejeitado
          emissor:produto-criado       → faturamento.on-produto-criado
          emissor:produto-atualizado   → faturamento.on-produto-atualizado
```

Entrega é at-least-once: todo consumidor é idempotente via `processed_messages`.
