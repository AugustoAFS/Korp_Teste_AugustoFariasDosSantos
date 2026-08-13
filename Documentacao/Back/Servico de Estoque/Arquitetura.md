# Arquitetura — Serviço de Estoque

Clean Architecture · .NET 10 · SQL Server

## Projetos

```
Estoque.Api
  Configurations/
    OpenApiConfig.cs            
    VersioningConfig.cs          
    RateLimitConfig.cs           
  Controllers/
    ProdutosController.cs
  Middlewares/
    ExceptionMiddleware.cs       
  Properties/
  Program.cs

Estoque.ApplicationService
  DependencyInjection/           
  Interfaces/
    IProdutoService.cs
    IBaixaEstoqueService.cs     
    IEstoqueEventPublisher.cs
  Services/            
  Helpers/

Estoque.Domain
  Dtos/
    Request/                      
    Response/                     
  Entities/                       
  Enums/                         
  Interfaces/
    IProdutoRepository.cs
    IMovimentoEstoqueRepository.cs
    IUnitOfWork.cs
    IOutboxRepository.cs 

Estoque.InfraStructure
  Data/
    Configurations/
  DependencyInjection/           
  Migrations/
  Repositories/

Estoque.EventListeners
  Messages/
    Publicados/                  
    Consumidos/                 
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
GET    /api/v1/produtos?pagina=1&tamanho=20&busca=          → PaginaDe<ProdutoDto>
GET    /api/v1/produtos/{id}
POST   /api/v1/produtos
PUT    /api/v1/produtos/{id}
```