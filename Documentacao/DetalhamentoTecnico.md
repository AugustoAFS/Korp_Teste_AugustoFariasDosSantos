# Detalhamento Técnico — Emissor NF

**Augusto Farias dos Santos** · [github.com/AugustoAFS/Korp_Teste_AugustoFariasDosSantos](https://github.com/AugustoAFS/Korp_Teste_AugustoFariasDosSantos)

Angular 21 · .NET 10 · PostgreSQL · SQL Server · RabbitMQ

Responde, na ordem, aos itens de detalhamento técnico da especificação.

---

## Cobertura da especificação

| Requisito | |
|---|---|
| Produto: código, descrição, saldo | ✅ |
| Nota: numeração sequencial, status Aberta/Fechada | ✅ |
| Múltiplos produtos com quantidades | ✅ |
| Indicador de processamento | ✅ |
| Status → Fechada ao final | ✅ |
| Bloquear ação em nota ≠ Aberta | ✅ `409` |
| Baixa de saldo conforme a quantidade | ✅ |
| **Obrigatório 1** · Microsserviços (mín. 2) | ✅ três |
| **Obrigatório 2** · Falha com recuperação e feedback | ✅ |
| **Obrigatório 3** · Banco real | ✅ |
| **Opcional a** · Concorrência | ✅ |
| **Opcional b** · Inteligência Artificial | ✅ |
| **Opcional c** · Idempotência | ✅ |

**A arquitetura em uma frase:** três serviços com bancos próprios; o front só fala com o gateway; os serviços de negócio nunca se chamam por HTTP — conversam por RabbitMQ, e cada um grava o evento na própria tabela de outbox **na mesma transação** do dado que mudou.

---

## 1 · Ciclos de vida do Angular

**O projeto não usa `ngOnInit` nem `ngOnDestroy` em lugar nenhum** — e isso é deliberado, não omissão. O Angular 21 com signals torna os hooks clássicos desnecessários: onde o código tradicional faria `ngOnInit` para buscar dados, aqui o recurso HTTP é um signal que se rebusca sozinho quando seus parâmetros mudam.

| Recurso | Onde | Para quê |
|---|---|---|
| `constructor` | 4 componentes | Injeção e registro de efeitos |
| `effect()` | coachmark, produto-form | Reagir a signal: reposicionar o balão do tour, preencher o formulário |
| `afterNextRender()` | layout | Disparar o tour **depois** que o DOM existe — os passos ancoram em elementos reais |
| `DestroyRef` + `takeUntilDestroyed()` | nota-detalhe | Encerrar o polling quando o usuário sai da tela |

O `effect` com `onCleanup` no coachmark substitui o par `ngAfterViewInit` + `ngOnDestroy` que registraria e removeria listeners de `resize`/`scroll` — com a vantagem de o cleanup rodar também **entre execuções**, não só na destruição.

A aplicação usa **roteamento por layout**: sidebar, header e notificações são instanciados uma vez em toda a sessão. Navegar entre telas não os reconstrói.

---

## 2 · RxJS

Usado cirurgicamente: **estado é signal, evento contínuo é Observable**.

O caso central é o acompanhamento da impressão. A API responde `202` na hora — a baixa corre pela saga — e a tela acompanha até o desfecho:

```ts
timer(1500, 1500).pipe(
  switchMap(() => this.http.get<Invoice>(`${API}/notas/${id}`)),
  takeWhile(nota => nota.printing, true)
)
```

- `switchMap` — se uma resposta demorar, a próxima pergunta **cancela** a anterior, evitando resposta fora de ordem
- `takeWhile(..., true)` — para sozinho ao terminar, e o `true` faz emitir **também** o valor final, que é o que a tela precisa mostrar

O critério de parada é `printing === false`, não a presença de `processingId` — de propósito: uma impressão que expira **conserva** o `processingId` para a auto-cura (item 7), então parar por ele deixaria a tela girando para sempre.

Também há `catchError`/`throwError` no interceptor global, `toSignal` para os eventos do Router, e `firstValueFrom` (19 ocorrências) onde há resposta única — ali a Promise é o modelo mais honesto.

---

## 3 · Outras bibliotecas

**Backend** — MassTransit + RabbitMQ (mensageria), YARP (proxy do gateway, com transform que troca o cookie por JWT interno), Polly (circuit breaker por cluster), EF Core com Npgsql e SqlServer, EFCore.NamingConventions (`snake_case` automático), Konscious.Argon2 (hash com pepper), QuestPDF (PDF da nota), Asp.Versioning, Scalar (documentação sobre OpenAPI), DataProtection.EFCore (cookie sobrevive a restart).

**Frontend** — Angular 21, RxJS 7.8, `@angular/cdk` (acessibilidade e overlay), Vitest, Prettier.

---

## 4 · Bibliotecas de componentes visuais

**O Angular Material está no projeto, mas nenhum componente Material é usado.** A dependência entra só pelo `mat.theme`, que gera cores, tipografia e densidade a partir de duas paletas — aproveitando o motor de tema, incluindo claro/escuro nativos, sem herdar a aparência genérica do Material Design.

Todos os componentes foram construídos do zero em `src/app/design-system/`: `toast`, `confirm`, `skeleton`, `empty-state`, `coachmark` e `logo`. O mesmo vale para tabelas, formulários, navegação e modais.

A razão é o requisito de identidade: o produto tem **dois territórios visuais distintos** — Estoque (chão de fábrica: laranja, cantos duros, monoespaçada) e Faturamento (documento formal: azul, serifada, mais respiro) — implementados por tokens de dialeto trocados pelo atributo `data-dial` na rota, sobre grade de 8 px. Sobrescrever um kit pronto daria mais trabalho que construir os seis componentes que o projeto usa.

---

## 5 · Gerenciamento de dependências no Golang

**Não aplicável** — o backend é inteiramente C# / .NET 10, e a especificação apresenta Golang e C# como alternativas.

Em .NET isso é feito por `PackageReference` nos `.csproj`, com restauração via NuGet. As soluções usam o formato `.slnx`.

---

## 6 · Frameworks no C#

.NET 10 · ASP.NET Core (MVC) · Entity Framework Core 10 · MassTransit 8.5 · YARP 2.3 · Polly.

Estoque e Faturamento seguem **Clean Architecture**, com a direção de dependência garantida pelas referências de projeto:

```
Api                 →  ApplicationService · InfraStructure · EventListeners · Ai
ApplicationService  →  Domain
InfraStructure      →  Domain
EventListeners      →  ApplicationService · InfraStructure
Domain              →  (nada)
```

O `Domain` não referencia nada: contratos como `IOutboxRepository` vivem nele, implementações ficam em `InfraStructure`, e o MassTransit fica confinado em `EventListeners`.

O **Gateway não usa Clean Architecture, e é intencional**: ele não tem domínio próprio. É autenticação e roteamento; quatro camadas ali seriam cerimônia vazia.

---

## 7 · Erros e exceções no backend

**Exceção não é fluxo de controle.** Nada que o sistema espera — nota inexistente, saldo insuficiente, nota já fechada — é sinalizado com `throw`.

Toda operação devolve `Result` ou `Result<T>`, carregando o valor ou um `Error` de um **catálogo estático** (`InvoiceNotFound`, `InvoiceAlreadyClosed`, `InvoiceEmpty`…), cada um já com status HTTP e a mensagem que o usuário lê. O controller tem uma linha:

```csharp
public async Task<IActionResult> GetInvoiceById(long id, CancellationToken ct)
    => Respond(await notaService.GetInvoiceById(id, ct));
```

`Respond` converte em **RFC 7807 ProblemDetails** com duas extensões: `code` (identificador estável e legível por máquina, que o front usa sem depender de texto) e `traceId` (correlação com o log).

Um `ExceptionMiddleware` cobre o inesperado e distingue dois casos: cancelamento de requisição pelo navegador vira log informativo — é comportamento normal, não incidente — e o resto vira log de erro mais um 500 genérico. **Nenhuma exceção vaza stack trace.**

**Falha entre serviços**, em três camadas:

- **Circuit breaker (Polly)** — um pipeline por cluster: 50 % de falha em 20 s, mínimo de 5 requisições, 15 s aberto. Com o Estoque fora, as chamadas caem de ~4 s para ~0,22 s. É **por cluster**: Estoque indisponível não afeta o Faturamento. Não há retry, de propósito — o gateway proxia POSTs não idempotentes e a saga já reprocessa. Circuito aberto vira **503 `service_unavailable`**, não o 502 sem corpo do YARP.
- **Tudo-ou-nada com savepoint** — item sem saldo desfaz as baixas parciais mas **preserva** o marcador de idempotência e o evento de rejeição.
- **Auto-cura** — o worker de expiração marca `last_error` em notas presas há mais de 60 s mas **nunca limpa o `processing_id`**. É essa decisão que faz um resultado atrasado ainda encontrar a nota, e a retentativa do usuário republicar sob a **mesma** chave.

**Idempotência com replay** *(opcional c)* — a entrega é at-least-once. A tabela `processed_messages` guarda o desfecho na mesma transação da baixa; uma mensagem duplicada **não é ignorada**: o resultado guardado é reemitido. Sem isso, um evento perdido deixaria a nota travada para sempre, porque a retentativa seria deduplicada e nada seria republicado.

---

## 8 · LINQ

Usado em **33 arquivos**. Todos os repositórios são escritos em LINQ; nenhuma query é montada como string.

O mais importante do projeto é a baixa de saldo:

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

É um `UPDATE ... WHERE balance >= @quantity` **atômico**, sem ler-modificar-gravar. A condição está no `WHERE`, então **o banco decide o vencedor**: `affected == 0` significa "não havia saldo" e é indistinguível — corretamente — de "outra transação chegou primeiro".

**É assim que o opcional (a) de concorrência é atendido.** Duas notas disputando um produto com saldo 1: uma atualiza uma linha, a outra atualiza zero e é rejeitada. Sem lock de aplicação, sem serializar a fila — por isso o consumidor roda com `ConcurrentMessageLimit = 5`. A constraint `ck_products_balance` é a rede final: mesmo que uma regra da aplicação falhasse, o banco recusaria saldo negativo.

Outros usos: filtro composto com `switch expression` que vira SQL, paginação com `Skip`/`Take`, `AsSplitQuery` para evitar explosão cartesiana no `Include`, e `ExecuteUpdateAsync` em lote no dispatcher do outbox. Na camada de aplicação, LINQ to Objects para projeção em DTOs e agregações.

---

## 9 · Inteligência Artificial *(opcional b)*

Duas funcionalidades, no projeto `Faturamento.Ai` — biblioteca própria, no mesmo padrão do `EventListeners`.

**A arquitetura manda no modelo, não o contrário.** As features falam com a porta `IChatModel` e não conhecem fornecedor; quem conhece protocolo é um único arquivo. A biblioteca **não tem dependência de SDK de fornecedor** — só `HttpClient` e `System.Text.Json`.

**a) Montagem da nota em linguagem natural.** O usuário escreve *"3 parafusos sextavados e dois martelos"* e recebe os itens resolvidos contra o catálogo, para confirmar.

A IA **nunca escreve**: o endpoint devolve uma proposta; quem grava continua sendo o `POST /notas/{id}/itens` que já existia, com todas as validações e a auditoria intactas. E **alucinação de produto é estruturalmente impossível** — o modelo não vê nem devolve `Guid`: recebe `código + descrição`, devolve `código + quantidade`, e o servidor resolve num dicionário. Código fora do catálogo volta como "não reconhecido".

**b) Tradução da recusa do estoque.** O motivo técnico vira uma frase para o usuário, gravada em campo próprio; `last_error` **continua técnico**, para log. A chamada é *best-effort* e fica **fora da transação**: qualquer falha vira `null` e a rejeição é aplicada de qualquer jeito. Indisponibilidade do fornecedor não pode travar a saga.

**Trocar de modelo é editar dois valores** em `appsettings.json`, sem código:

| | `BaseUrl` | `Model` |
|---|---|---|
| Gemini | `…/v1beta/openai` | `gemini-3.6-flash` |
| Anthropic | `https://api.anthropic.com/v1` | `claude-haiku-4-5` |
| OpenAI | `https://api.openai.com/v1` | `gpt-4o-mini` |
| Ollama local | `http://localhost:11434/v1` | `llama3.2` |

**Sem chave, todo o resto funciona** — só os recursos de IA respondem `503 ai_disabled`, e o boot avisa nomeando `Ai:ApiKey`. É a única exceção à regra "sem fallback" do projeto, e ela é barulhenta em vez de silenciosa.

Na interface, tudo que vem de IA é marcado com **✨ e roxo** — borda, fundo e rótulo — para o usuário nunca confundir sugestão de modelo com dado do sistema.

---

## Testes

Mais de **700**, nos quatro projetos. Os de integração sobem PostgreSQL, SQL Server e RabbitMQ **de verdade**, em contêineres descartáveis — inclusive o de concorrência, que roda baixas simultâneas contra o banco real, e o de idempotência, que entrega a mesma mensagem duas vezes e verifica que o saldo caiu uma só.

---

## Configuração

Não há *user secrets*: connection strings e chaves de desenvolvimento estão versionadas, de propósito, para o projeto rodar com `git clone` e um comando.

A configuração é **obrigatória e validada no boot** — seção ausente derruba a inicialização nomeando a chave, em vez de subir meio configurada. A única exceção é `Ai:ApiKey`.

O banco sobe **vazio**, exceto pela conta administrativa. Nenhum dado de exemplo é semeado.
