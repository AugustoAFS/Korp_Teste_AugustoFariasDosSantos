# Detalhamento Técnico — Sistema de Emissão de Notas Fiscais

**Candidato:** Augusto Farias dos Santos
**Repositório:** https://github.com/AugustoAFS/Korp_Teste_AugustoFariasDosSantos
**Stack:** Angular 21 · .NET 10 · PostgreSQL · SQL Server · RabbitMQ

Este documento responde, na ordem, aos oito itens de detalhamento técnico pedidos na especificação do desafio.

| # | Item da especificação |
|---|---|
| — | [Cobertura da especificação](#cobertura-da-especificação) |
| — | [Visão geral da solução](#visão-geral-da-solução) |
| 01 | [Ciclos de vida do Angular utilizados](#01--ciclos-de-vida-do-angular-utilizados) |
| 02 | [Uso da biblioteca RxJS](#02--uso-da-biblioteca-rxjs) |
| 03 | [Outras bibliotecas e finalidade](#03--outras-bibliotecas-e-finalidade) |
| 04 | [Bibliotecas de componentes visuais](#04--bibliotecas-de-componentes-visuais) |
| 05 | [Gerenciamento de dependências no Golang](#05--gerenciamento-de-dependências-no-golang) |
| 06 | [Frameworks utilizados no C#](#06--frameworks-utilizados-no-c) |
| 07 | [Tratamento de erros e exceções no backend](#07--tratamento-de-erros-e-exceções-no-backend) |
| 08 | [Uso de LINQ](#08--uso-de-linq) |
| — | [Notas sobre execução](#notas-sobre-execução) |

---

## Cobertura da especificação

Onde cada requisito do desafio foi atendido, incluindo os opcionais.

### Funcionalidades

| Requisito | Situação |
|---|---|
| Produto com código, descrição e saldo | **Atendido** |
| Nota fiscal com numeração sequencial | **Atendido** |
| Status Aberta / Fechada | **Atendido** |
| Inclusão de múltiplos produtos com quantidades | **Atendido** |
| Botão de impressão visível e intuitivo | **Atendido** |
| Indicador de processamento durante a impressão | **Atendido** |
| Status atualizado para Fechada ao final | **Atendido** |
| Impressão bloqueada para nota não-Aberta | **Atendido** |
| Baixa de saldo conforme a quantidade da nota | **Atendido** |

### Requisitos obrigatórios

| Requisito | Situação |
|---|---|
| Arquitetura de microsserviços — mínimo dois *(entregues três)* | **Atendido** |
| Tratamento de falhas com recuperação e feedback ao usuário | **Atendido** |
| Conexão real com banco de dados | **Atendido** |

### Requisitos opcionais

| Requisito | Situação |
|---|---|
| **a.** Tratamento de concorrência | **Atendido** |
| **c.** Implementação de idempotência | **Atendido** |
| **b.** Uso de Inteligência Artificial | *Não implementado* |

---

## Visão geral da solução

Três serviços de backend e um frontend, todos em containers e acessíveis por uma única URL.

| Serviço | Papel | Banco |
|---|---|---|
| `API-Gateway` | Ponto único de entrada: autenticação, proxy reverso e circuit breaker | PostgreSQL |
| `API-Faturamento` | Notas fiscais, itens e ciclo de impressão | PostgreSQL |
| `API-Estoque` | Autoridade sobre o saldo dos produtos | SQL Server |
| `NotaFlow` | Interface do usuário em Angular | — |

```
docker compose --profile app up -d --build   →   http://localhost:5000
```

O navegador conversa **apenas com o gateway**. Ele serve o frontend em `/` e faz proxy de `/api/v1/produtos` para o Estoque e `/api/v1/notas` para o Faturamento. Origem única significa ausência de CORS e um cookie de sessão `SameSite=Strict` que simplesmente funciona.

### A impressão é uma saga coreografada

Os dois serviços de negócio nunca se chamam por HTTP.

```
Faturamento              RabbitMQ                Estoque
    │                                              │
    ├── BaixarEstoqueCommand ────────────────────► │
    │                                              ├─ debita tudo-ou-nada
    │ ◄──────── EstoqueBaixado / EstoqueRejeitado ─┤
    ├─ fecha ou rejeita a nota                     │
```

Cada serviço escreve o evento na **própria tabela de outbox dentro da mesma transação** do dado que mudou; um worker publica no RabbitMQ depois. É isso que garante que "saldo debitado" e "evento publicado" nunca divergem, mesmo se o processo morrer entre os dois.

---

## 01 — Ciclos de vida do Angular utilizados

Esta é a resposta que mais merece contexto, porque o projeto **não usa `ngOnInit` nem `ngOnDestroy` em lugar nenhum** — e isso é deliberado, não omissão.

O Angular 21 oferece um modelo reativo baseado em signals que torna os hooks clássicos desnecessários na maior parte dos casos. Onde o código tradicional faria `ngOnInit` para buscar dados, aqui o próprio recurso HTTP é declarado como um signal que se rebusca sozinho quando seus parâmetros mudam. Os pontos do ciclo de vida efetivamente usados são:

| Recurso | Onde | Para quê |
|---|---|---|
| `constructor` | 4 componentes | Injeção e registro de efeitos |
| `effect()` | [coachmark.component.ts:23](../NotaFlow/src/app/design-system/coachmark/coachmark.component.ts#L23)<br>[produto-form.component.ts:32](../NotaFlow/src/app/pages/produtos/produto-form/produto-form.component.ts#L32) | Reagir a mudança de signal — reposicionar o balão do tour, preencher o formulário quando o produto editado troca |
| `afterNextRender()` | [layout.component.ts:28](../NotaFlow/src/app/layout/layout.component.ts#L28) | Disparar o tour de onboarding depois que o DOM existe — os passos ancoram em elementos reais |
| `DestroyRef` + `takeUntilDestroyed()` | [nota-detalhe.component.ts:139](../NotaFlow/src/app/pages/notas/nota-detalhe/nota-detalhe.component.ts#L139) | Encerrar o polling da impressão quando o usuário sai da tela |

O `effect` com `onCleanup` no coachmark merece destaque: ele substitui o par `ngAfterViewInit` + `ngOnDestroy` que seria necessário para registrar e remover listeners de `resize` e `scroll`, com a vantagem de o cleanup rodar também **entre execuções**, não só na destruição.

> **Roteamento baseado em layout.** A aplicação usa uma rota-mãe `LayoutComponent` com as páginas como `children` ([app.routes.ts](../NotaFlow/src/app/app.routes.ts)). Sidebar, header e sistema de notificação são instanciados **uma única vez** em toda a sessão — navegar entre telas não os reconstrói. Isso muda o ciclo de vida real da aplicação: os componentes comuns nunca são destruídos.

---

## 02 — Uso da biblioteca RxJS

Sim — mas de forma cirúrgica, apenas onde o problema é genuinamente de fluxo assíncrono ao longo do tempo. Estado é signal; evento contínuo é Observable.

### a) Polling da impressão — o caso mais interessante

[invoices.service.ts:29-31](../NotaFlow/src/app/core/services/invoices.service.ts#L29-L31)

```ts
timer(1500, 1500).pipe(
  switchMap(() => this.http.get<Invoice>(`${API}/notas/${id}`)),
  takeWhile(nota => nota.printing, true)
)
```

Quando o usuário clica em imprimir, a API responde `202 Accepted` imediatamente — a baixa de estoque acontece de forma assíncrona pela saga. O front então acompanha a nota até o desfecho. Três operadores, três responsabilidades:

- `timer(1500, 1500)` — pergunta a cada 1,5 s;
- `switchMap` — se uma resposta demorar, a próxima pergunta **cancela** a anterior, evitando resposta fora de ordem;
- `takeWhile(nota => nota.printing, true)` — para sozinho quando a impressão termina. O `true` no segundo argumento faz emitir **também** o valor final, que é justamente o que a tela precisa mostrar: nota fechada ou rejeitada.

> O critério de parada é `printing === false`, e não a presença de `processingId` — de propósito. Uma impressão que expira **conserva** o `processingId` para permitir a auto-cura da saga (item 07), então parar por esse campo deixaria a tela girando para sempre.

### b) Interceptação global de erros

[error.interceptor.ts:13](../NotaFlow/src/app/core/interceptors/error.interceptor.ts#L13) — `catchError` + `throwError` traduzem o `ProblemDetails` do backend em uma notificação legível e reencaminham o erro para quem chamou.

### c) `toSignal`

[app.component.ts:19](../NotaFlow/src/app/app.component.ts#L19) — converte os eventos de navegação do Router em signal, a ponte entre o mundo Observable do framework e o mundo signal da aplicação.

### d) `firstValueFrom` — 19 ocorrências

Para requisições de resultado único, a Promise é o modelo mais honesto: não há stream, há uma resposta. Usar Observable ali seria cerimônia sem ganho.

---

## 03 — Outras bibliotecas e finalidade

### Backend

| Biblioteca | Versão | Finalidade |
|---|---|---|
| MassTransit + RabbitMQ | 8.5.10 | Mensageria: publicação, consumo, retry e definição de filas por convenção |
| YARP | 2.3.0 | Proxy reverso no gateway, com transform que troca o cookie de sessão por um JWT interno assinado |
| Http.Resilience (Polly) | 10.9.0 | Circuit breaker por cluster downstream |
| Entity Framework Core | 10.0.11 | ORM, migrations, transações e savepoints |
| Npgsql for EF Core | 10.0.3 | Provider PostgreSQL — Gateway e Faturamento |
| EF Core SqlServer | 10.0.11 | Provider SQL Server — Estoque |
| EFCore.NamingConventions | 10.0.1 | Converte entidades PascalCase em tabelas e colunas `snake_case` |
| Konscious Argon2 | 1.3.1 | Hash de senha com Argon2id, com pepper vindo de configuração |
| IdentityModel.JsonWebTokens | 8.22.0 | Emissão do JWT interno gateway → serviços |
| Asp.Versioning.Mvc | 10.2.1 | Versionamento de API por rota |
| Scalar.AspNetCore | 2.16.20 | Documentação interativa sobre o OpenAPI |
| DataProtection.EFCore | 10.0.11 | Persistência das chaves, para o cookie sobreviver a restart |

### Frontend

| Biblioteca | Versão | Finalidade |
|---|---|---|
| Angular | 21.2 | Framework — standalone components, signals, `httpResource` |
| RxJS | 7.8 | Ver item 02 |
| @angular/cdk | 21.2 | Primitivas de acessibilidade e overlay |
| @angular/material | 21.2 | Somente o sistema de tema e paleta — ver item 04 |
| Vitest | 4.0 | Testes de componente — 23 testes |
| Prettier | 3.8 | Formatação |

---

## 04 — Bibliotecas de componentes visuais

O Angular Material está no projeto, mas **nenhum componente Material é usado**.

A dependência entra exclusivamente pelo `mat.theme` em [styles.scss:16](../NotaFlow/src/styles.scss#L16), que gera o sistema de cores, tipografia e densidade a partir de duas paletas — aproveitando o motor de tema, incluindo claro e escuro nativos via `color-scheme`, sem herdar a aparência genérica do Material Design.

**Todos os componentes visuais foram construídos do zero**, em [src/app/design-system/](../NotaFlow/src/app/design-system/): `toast`, `confirm`, `skeleton`, `empty-state`, `coachmark` e `logo` — o SVG da marca. O mesmo vale para tabelas, formulários, navegação e modais.

A razão é o requisito de identidade visual do produto: o sistema tem **dois territórios com linguagens visuais distintas**.

| Território | Linguagem |
|---|---|
| **Estoque** | Chão de fábrica: laranja, cantos duros, tipografia monoespaçada para códigos |
| **Faturamento** | Documento formal: azul, serifada, mais respiro entre os elementos |

Isso é implementado por *design tokens* de dialeto (`--d-*`) trocados pelo atributo `data-dial` na rota, sobre uma grade de 8 px. Sobrescrever um kit pronto para chegar nesse resultado daria mais trabalho — e mais CSS — do que construir os seis componentes que o projeto realmente usa.

O onboarding — coachmark e tour de 9 passos — também é próprio, sem biblioteca de terceiros.

---

## 05 — Gerenciamento de dependências no Golang

**Não aplicável.** O backend foi implementado inteiramente em C# / .NET 10, e a especificação apresenta Golang e C# como alternativas.

Para registro: em .NET o gerenciamento de dependências é feito por `PackageReference` nos arquivos `.csproj`, com restauração via NuGet — `dotnet restore`, implícito no `dotnet build`. As soluções usam o formato `.slnx`, mais recente, e não há arquivos `.sln` no repositório.

---

## 06 — Frameworks utilizados no C#

- **.NET 10** — runtime e SDK
- **ASP.NET Core (MVC/Controllers)** — camada de API dos três serviços
- **Entity Framework Core 10** — acesso a dados, migrations, transações e savepoints
- **MassTransit 8.5** — mensageria sobre RabbitMQ
- **YARP 2.3** — proxy reverso do gateway
- **Polly** — resiliência, via `Microsoft.Extensions.Http.Resilience`

### Arquitetura

Estoque e Faturamento seguem **Clean Architecture**, com a direção de dependência garantida pelas referências de projeto:

```
Api                 →  ApplicationService · InfraStructure · EventListeners
ApplicationService  →  Domain
InfraStructure      →  Domain
EventListeners      →  ApplicationService · InfraStructure
Domain              →  (nada)
```

O `Domain` não referencia nada — contratos como `IOutboxRepository` vivem nele, e as implementações ficam em `InfraStructure`. Contratos de mensagem e o wiring do MassTransit ficam confinados em `EventListeners`.

O Gateway **não** usa Clean Architecture, e isso é intencional: ele não tem domínio próprio. É autenticação e roteamento; uma estrutura de quatro camadas ali seria cerimônia vazia.

---

## 07 — Tratamento de erros e exceções no backend

O princípio central: **exceção não é fluxo de controle**. Nada que o sistema espera que aconteça — nota inexistente, saldo insuficiente, nota já impressa — é sinalizado com `throw`.

### 7.1 Result como retorno de negócio

Toda operação de serviço devolve `Result` ou `Result<T>` ([Result.cs](../API-Faturamento/Faturamento.Domain/Dtos/Result.cs)), carregando ou o valor de sucesso ou um `Error`:

```csharp
public sealed record Error
{
    public string Code { get; init; }
    public string Title { get; init; }
    public HttpStatusCode Status { get; init; }
    public string Detail { get; init; }

    public Error With(string detail) => this with { Detail = detail };
}
```

Os erros são um **catálogo estático** ([Errors.cs](../API-Faturamento/Faturamento.Domain/Exceptions/Errors.cs)) — `InvoiceNotFound`, `InvoiceNotEditable`, `InvoiceAlreadyPrinting`, `InvoiceAlreadyClosed`, `InvoiceEmpty`. Cada um já nasce com o status HTTP e a mensagem que o usuário final vai ler. `With()` permite enriquecer o detalhe sem duplicar a definição.

### 7.2 Tradução única para HTTP

O controller não decide status code. Ele tem uma linha:

```csharp
public async Task<IActionResult> GetInvoiceById(long id, CancellationToken ct)
    => Respond(await notaService.GetInvoiceById(id, ct));
```

`Respond` ([BaseController.cs](../API-Faturamento/Faturamento.Api/Controllers/BaseController.cs)) converte o `Result` em **RFC 7807 ProblemDetails**, acrescentando duas extensões:

- `code` — identificador estável e legível por máquina, como `invoice_already_closed`, que o front usa para decidir comportamento sem depender de texto;
- `traceId` — correlação com os logs.

### 7.3 Rede de segurança para o inesperado

O `ExceptionMiddleware` ([ExceptionMiddleware.cs](../API-Faturamento/Faturamento.Api/Middlewares/ExceptionMiddleware.cs)) envolve o pipeline e trata dois casos distintos:

```
catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    → log informativo, sem erro (o usuário só fechou a aba)

catch (Exception ex)
    → log de erro + ProblemDetails 500 genérico
```

A distinção importa: cancelamento de requisição é comportamento normal do navegador, não incidente. Tratá-lo como erro polui o log justamente quando ele mais é necessário. Nenhuma exceção não tratada vaza stack trace para o cliente.

### 7.4 Falha entre serviços — três camadas

**Circuit breaker (Polly).** [ResilienceConfig.cs](../API-Gateway/Gateway/Config/ResilienceConfig.cs) registra um pipeline por cluster downstream: 50 % de falha em janela de 20 s, mínimo de 5 requisições, 15 s aberto, timeout de 10 s dentro do breaker. Com o Estoque fora do ar, as chamadas caem de ~4 s para ~0,22 s após a quinta falha — o usuário recebe resposta imediata em vez de esperar timeout. O breaker é **por cluster**: Estoque indisponível não afeta o Faturamento.

> **Não há retry, deliberadamente.** O gateway faz proxy de POSTs não idempotentes, e a saga já reprocessa na camada de mensagem. Quando o circuito está aberto, o YARP devolveria um 502 sem corpo — o gateway inspeciona `IForwarderErrorFeature` e reescreve como **503 com `code: service_unavailable`**, para que o front tenha o que mostrar.

**Tudo-ou-nada com savepoint.** Se uma nota tem cinco itens e o terceiro não tem saldo, o consumidor do Estoque usa um savepoint para desfazer as baixas parciais, mas **preservar** o marcador de idempotência e o evento de rejeição. A nota inteira é rejeitada, sem debitar nada.

**Auto-cura da saga.** O `PrintExpirationWorker` marca `last_error` em notas imprimindo há mais de 60 s, mas **nunca limpa o `processing_id`**. Essa decisão é o que torna a saga auto-recuperável: um `EstoqueBaixado` que chega atrasado ainda encontra a nota e se aplica, e a retentativa do usuário republica sob a mesma chave.

### 7.5 Idempotência com replay do desfecho

A entrega é **at-least-once**, então todo consumidor precisa ser idempotente. A implementação vai além do dedup ingênuo: a tabela `processed_messages` do Estoque guarda `outcome_type` e `outcome_payload` **na mesma transação** da baixa.

Quando uma `BaixarEstoqueCommand` duplicada chega, o consumidor **não a ignora** — ele reenfileira o resultado armazenado. A razão é concreta: se um evento de resultado se perdesse definitivamente, um consumidor que apenas engolisse a duplicata deixaria a nota travada para sempre, porque a retentativa seria deduplicada e nada seria republicado.

> Esse cenário foi validado **removendo a binding do RabbitMQ** para perder o evento de verdade, e confirmando que a retentativa curou a nota com o saldo debitado exatamente uma vez.

---

## 08 — Uso de LINQ

Sim, extensivamente — **33 arquivos** — em duas modalidades.

### LINQ to Entities, traduzido para SQL

Todos os repositórios são escritos em LINQ; nenhuma query SQL é montada como string.

#### a) Filtro composto com paginação

[InvoiceRepository.cs:12-43](../API-Faturamento/Faturamento.InfraStructure/Repositories/InvoiceRepository.cs#L12-L43) — a query é montada por composição, e um `switch expression` escolhe o predicado da situação. Nada é materializado antes da hora:

```csharp
var query = context.Invoices.AsNoTracking();

if (onlyUserId is not null)
    query = query.Where(i => i.IssuedByUserId == onlyUserId);

query = filter.Situation switch
{
    InvoiceSituation.Open     => query.Where(i => i.Status == InvoiceStatus.Open
                                              && i.ProcessingId == null && i.LastError == null),
    InvoiceSituation.Printing => query.Where(i => i.Status == InvoiceStatus.Open
                                              && i.ProcessingId != null && i.LastError == null),
    InvoiceSituation.Pending  => query.Where(i => i.Status == InvoiceStatus.Open
                                              && i.LastError != null),
    InvoiceSituation.Closed   => query.Where(i => i.Status == InvoiceStatus.Closed),
    _ => query
};

var total = await query.CountAsync(ct);

var items = await query
    .Include(i => i.Items)
    .OrderByDescending(i => i.Number)
    .Skip((filter.Page - 1) * filter.Size)
    .Take(filter.Size)
    .AsSplitQuery()
    .ToListAsync(ct);
```

O `AsSplitQuery()` evita a explosão cartesiana: com `Include` em uma tabela filha, um único JOIN duplicaria os dados da nota por item.

#### b) Baixa de saldo — o LINQ mais importante do projeto

[ProductRepository.cs:43-61](../API-Estoque/Estoque.InfraStructure/Repositories/ProductRepository.cs#L43-L61)

```csharp
var affected = await context.Products
    .Where(p => p.Id == productId && p.Active && p.Balance >= quantity)
    .ExecuteUpdateAsync(
        update => update
            .SetProperty(p => p.Balance, p => p.Balance - quantity)
            .SetProperty(p => p.UpdatedAt, now),
        ct);

if (affected == 0) return null;
```

Isto é um `UPDATE ... WHERE balance >= @quantity` **atômico**, sem ler-modificar-gravar. A condição de saldo está no `WHERE`, então o banco decide o vencedor: `affected == 0` significa "não havia saldo" e é indistinguível — corretamente — de "outra transação chegou primeiro".

> **É assim que o requisito opcional de concorrência é atendido.** Duas notas disputando um produto com saldo 1: uma atualiza uma linha, a outra atualiza zero e é rejeitada. Sem lock de aplicação, sem serialização de fila — por isso o consumidor roda com `ConcurrentMessageLimit = 5` ([OnBaixarEstoque.cs:33](../API-Estoque/Estoque.EventListeners/Listeners/OnBaixarEstoque.cs#L33)) em vez de processar um de cada vez. A constraint `ck_products_balance` (`[balance] >= 0`, em [ProductConfiguration.cs:11](../API-Estoque/Estoque.InfraStructure/Data/Configurations/ProductConfiguration.cs#L11)) é a rede de segurança final: mesmo que uma regra de aplicação falhasse, o banco recusaria o saldo negativo.

#### c) `ExecuteUpdateAsync` em lote

O dispatcher da outbox marca as mensagens publicadas em uma única instrução, sem materializar entidades ([OutboxRepository.cs:31](../API-Faturamento/Faturamento.InfraStructure/Repositories/OutboxRepository.cs#L31)).

### LINQ to Objects

Usado na camada de aplicação para transformação em memória — projeção de entidades para DTOs de resposta, agregações do dashboard e composição de claims na autenticação.

---

## Notas sobre execução

Não há uso de *user secrets*: connection strings e chaves de desenvolvimento estão versionadas no `appsettings.json` para que o projeto rode com `git clone` seguido de um único comando, sem etapa manual de configuração. São chaves de desenvolvimento, versionadas de propósito para facilitar a avaliação.

A configuração é **obrigatória e validada no boot** — não há valores padrão silenciosos nem fallbacks. Uma seção ausente ou inválida derruba a inicialização nomeando a chave responsável, em vez de deixar o sistema subir num estado meio configurado.

O banco sobe **vazio**, exceto pela conta administrativa de teste. Não há dados de exemplo semeados: todo cadastro visto na demonstração foi criado pela interface.
