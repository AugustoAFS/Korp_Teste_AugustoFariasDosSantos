# Arquitetura — Serviço de Faturamento

Clean Architecture · .NET 10 · PostgreSQL

## Projetos

```
Faturamento.Api
  Configurations/
    OpenApiConfig.cs               OpenAPI + Scalar
    VersioningConfig.cs            Asp.Versioning — versão na URL
    RateLimitConfig.cs             limitador global
  Controllers/
    NotasController.cs
  Middlewares/
    ExceptionMiddleware.cs
  Properties/
  Program.cs

Faturamento.ApplicationService
  DependencyInjection/             AddApplicationServices()
  Interfaces/
    INotaFiscalService.cs
    IImpressaoService.cs
    IFaturamentoEventPublisher.cs
  Services/                        casos de uso
  Helpers/

Faturamento.Domain
  Dtos/
    Request/                       NotaRequest · ItemNotaRequest
    Response/                      NotaFiscalResponse · PaginaDe<T>
  Entities/                        NotaFiscal · ItemNotaFiscal
  Enums/                           StatusNota
  Interfaces/
    INotaFiscalRepository.cs
    IProdutoReplicadoRepository.cs
    IUnitOfWork.cs
    IOutboxRepository.cs           + OutboxPendente

Faturamento.InfraStructure
  Data/
    Configurations/                AppDbContext · IEntityTypeConfiguration
  DependencyInjection/             AddInfraServices()
  Migrations/
  Repositories/

Faturamento.EventListeners         toda a mensageria
  Messages/
    Publicados/                    BaixarEstoqueCommand
    Consumidos/                    EstoqueBaixadoEvent · EstoqueRejeitadoEvent
                                   ProdutoCriadoEvent · ProdutoAtualizadoEvent
  Listeners/
    OnEstoqueBaixado.cs
    OnEstoqueRejeitado.cs
    OnProdutoCriado.cs
    OnProdutoAtualizado.cs
  Publishers/
    FaturamentoEventPublisher.cs   implementa IFaturamentoEventPublisher
  Workers/
    OutboxDispatcherWorker.cs
    ImpressaoExpiradaWorker.cs
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

```
GET    /api/v1/notas?pagina=1&tamanho=20&status=            → PaginaDe<NotaFiscalDto>
GET    /api/v1/notas/{id}
POST   /api/v1/notas
POST   /api/v1/notas/{id}/itens
DELETE /api/v1/notas/{id}/itens/{itemId}
POST   /api/v1/notas/{id}/imprimir     → 202 Accepted