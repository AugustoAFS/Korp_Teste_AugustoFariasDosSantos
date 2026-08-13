# Arquitetura — Front

Angular 20+ · standalone components · signals · Reactive Forms tipados · Angular Material

## Stack

| Peça | Escolha | Finalidade |
|---|---|---|
| Angular | 20+ standalone | sem NgModules |
| Angular Material + CDK | UI | tabela, dialog, snackbar, spinner |
| RxJS | fluxo assíncrono | polling, typeahead, retry |
| Signals | estado local e de serviço | sem NgRx |
| TypeScript strict | — | `strictTemplates` ligado |

Nenhuma biblioteca de estado externa. Store por serviço com `signal` + `computed` resolve nesta escala.

## Estrutura

```
src/app/
  core/
    interceptors/   credenciais.interceptor.ts · erro.interceptor.ts
    guards/         auth.guard.ts
    services/       auth.service.ts · produtos.service.ts · notas.service.ts
    models/         produto.ts · nota-fiscal.ts · problem-details.ts
  features/
    auth/           login.component.ts
    produtos/       produtos-lista.component.ts · produto-form.component.ts
    notas/          notas-lista.component.ts · nota-detalhe.component.ts
                    itens-nota.component.ts
  shared/
    status-chip.component.ts
  app.routes.ts
  app.config.ts
```

## Rotas

| Rota | Guard | Seção | Componente |
|---|---|---|---|
| `/login` | — | — | `LoginComponent` |
| `/produtos` | `authGuard` | produtos | `ProdutosListaComponent` |
| `/notas` | `authGuard` | notas | `NotasListaComponent` |
| `/notas/:id` | `authGuard` | notas | `NotaDetalheComponent` |
| `/` | — | — | redireciona para `/produtos` |

Lazy loading por rota com `loadComponent`. O `data.secao` de cada rota alimenta o tema por seção descrito em [DesignSystem.md](DesignSystem.md).

Guard na **forma funcional** (`CanActivateFn`) — guard de classe foi removido no Angular 17. Função pura, roda em contexto de injeção, sem provider: consulta o `AuthService` e devolve `true` ou um `UrlTree` para `/login`.

## Componentes

Wireframes e estados de cada tela em [Telas.md](Telas.md). Models e contrato com a API em [Domain.md](Domain.md).

```
LoginComponent                    smart    form + AuthService

ProdutosListaComponent            smart    tabela, busca, abre dialog
  └ ProdutoFormComponent          dumb     dialog, @Input produto | null

NotasListaComponent               smart    tabela, cria nota

NotaDetalheComponent              smart    carrega nota, imprime, faz polling
  ├ StatusChipComponent           dumb     @Input status, processando
  ├ AdicionarItemComponent        dumb     autocomplete + qtd, @Output adicionar
  └ ItensNotaComponent            dumb     @Input nota, @Output remover
```

Só os `smart` injetam service. Os `dumb` recebem por `@Input` e avisam por `@Output` — são `OnPush` puros e testáveis isolados.

## Ciclos de vida

| Hook | Onde | Por quê |
|---|---|---|
| `ngOnInit` | `ProdutosListaComponent`, `NotaDetalheComponent` | carga inicial dos dados |
| `ngAfterViewInit` | `ProdutoFormComponent` | foco no primeiro campo após o dialog renderizar |
| `ngOnChanges` | `ItensNotaComponent` | reage à troca do `@Input() nota` vinda do pai |
| `ngOnDestroy` | **não usado** | substituído por `takeUntilDestroyed(DestroyRef)` |

A ausência do `ngOnDestroy` é deliberada: o teardown das inscrições fica com `takeUntilDestroyed`, que amarra o ciclo do Observable ao do componente sem código manual.

## RxJS — onde e por quê

| Caso | Operadores | Motivo |
|---|---|---|
| Polling do status de impressão | `timer` · `switchMap` · `takeWhile(inclusive)` | acompanha a nota após o `202 Accepted` até o backend concluir |
| Typeahead de produtos | `debounceTime(300)` · `distinctUntilChanged` · `switchMap` | evita uma requisição por tecla no autocomplete |
| Falha transitória | `retry` com backoff exponencial | 3 tentativas antes de mostrar erro |
| Interceptors | `catchError` · `map` | `HttpInterceptorFn` opera sobre Observable |
| Carga paralela | `forkJoin` | nota e catálogo juntos na tela de detalhe |

O polling é o caso principal. `switchMap` descarta resposta atrasada se o próximo tick sair antes; `takeWhile` com `inclusive = true` encerra já entregando o estado final. Intervalo de 1,5s, encerrado quando `processamentoId` volta a `null`.

**RxJS para o fluxo, signal para o template** — a conversão acontece na borda do componente, com `toSignal`.

## Bibliotecas

### Componentes visuais — Angular Material

| Componente | Uso |
|---|---|
| `MatTable` + `MatSort` + `MatPaginator` | listagens de produtos e notas |
| `MatDialog` | cadastro e edição |
| `MatSnackBar` | feedback de erro e sucesso |
| `MatProgressSpinner` | **indicador de processamento da impressão** |
| `MatFormField`, `MatInput`, `MatSelect`, `MatAutocomplete` | formulários |
| `MatChip` | status Aberta / Fechada |
| `MatButton`, `MatIcon`, `MatToolbar` | navegação |

Escolhido por ser oficial, ter CDK junto e cobrir tudo do escopo sem dependência extra.

### Demais

| Biblioteca | Finalidade |
|---|---|
| RxJS | fluxo assíncrono |
| `@angular/forms` | Reactive Forms tipados |
| `@angular/animations` | transições da UI |

Lista curta de propósito — nenhuma dependência que o escopo não exigisse.

## Feedback de erro ao usuário

Requisito obrigatório nº 2 do PDF — "fornecer feedback apropriado ao usuário sobre o erro" — é satisfeito **aqui**, não no backend.

Um `erroInterceptor` global captura toda falha HTTP, traduz para linguagem de usuário e dispara o toast. Nenhum componente trata erro HTTP sozinho.

| Status | Mensagem |
|---|---|
| 503 | "Serviço de estoque indisponível. Sua nota continua aberta — tente novamente em instantes." |
| 429 | "Muitas requisições. Aguarde alguns instantes." — usa o header `Retry-After` |
| 409 | motivo do `ProblemDetails` (nota já fechada, conflito de concorrência) |
| 422 | erro de validação de domínio |
| 401 | redireciona para login |
| 0 / timeout | "Sem conexão com o servidor." |

Mensagens saem do `ProblemDetails` (RFC 7807) devolvido pelo backend — front e back falam o mesmo contrato de erro.

## Autenticação

Zero gerenciamento de token. O cookie é `HttpOnly`, o navegador envia sozinho.

| Peça | Papel |
|---|---|
| `credenciaisInterceptor` | anexa `withCredentials: true` em toda requisição |
| `withXsrfConfiguration` | cookie `XSRF-TOKEN` → header `X-XSRF-TOKEN` |
| `proxy.conf.json` | aponta `/api` para o gateway em `localhost:5000` |

Sem serviço de token, sem refresh, sem `localStorage`.

O proxy faz o front rodar same-origin já em desenvolvimento — condição para a proteção XSRF nativa do Angular anexar o header.

## Estado

Signals em serviço, sem NgRx. Cada service expõe o sinal privado como `asReadonly()` e deriva o que a tela precisa com `computed` — quem consome não escreve.

`OnPush` em todos os componentes: com signals é o padrão correto e evita ciclos de verificação desnecessários.
