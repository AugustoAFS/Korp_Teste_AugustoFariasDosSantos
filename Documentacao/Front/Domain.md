# Domínio — Front

Models TypeScript e contrato com a API. Tudo passa pelo gateway em `/api/v1`.

> **O JSON da API é em inglês.** Campos, rotas e códigos de erro seguem o idioma do backend
> (ver `CLAUDE.md`). Só texto que chega ao usuário — `title` e `detail` do `ProblemDetails` —
> fica em português. Os models do front espelham o payload, sem tradutor no meio.

> Este contrato reflete o backend **em execução** — todos os itens abaixo estão implementados e
> validados fim a fim. A única peça pendente é o `proxy.conf.json` do front, marcado com 🕓.

## Models

### Product

| Campo | Tipo | Nota |
|---|---|---|
| `id` | `string` | UUID v7 |
| `code` | `string` | — |
| `description` | `string` | — |
| `balance` | `number` | inteiro `>= 0` |
| `active` | `boolean` | — |

`CreateProductRequest` leva `code`, `description` e `balance` — o saldo é o inicial.
`UpdateProductRequest` leva `code`, `description` e `active`: saldo não muda por edição de
cadastro, só pela baixa da nota.

### Invoice

| Campo | Tipo | Nota |
|---|---|---|
| `id` | `number` | **BIGINT**, não UUID |
| `number` | `number` | gerado pelo backend, sequência própria |
| `status` | `'Open' \| 'Closed'` | enum serializado como string |
| `issuedByUserName` | `string` | snapshot do claim `name` |
| `createdAt` | `string` | ISO |
| `closedAt` | `string \| null` | — |
| `processingId` | `string \| null` | UUID — **não use para saber se está imprimindo** |
| `printing` | `boolean` | calculado pelo backend |
| `editable` | `boolean` | calculado pelo backend |
| `lastError` | `string \| null` | motivo da rejeição **ou** da expiração |
| `items` | `InvoiceItem[]` | — |

**`printing` e `editable` são a fonte da verdade para a tela.** A regra intuitiva
`processingId != null → imprimindo` está **errada**: quando uma impressão expira, o backend
preenche `lastError` mas **preserva** o `processingId` de propósito — é isso que permite a nota
se curar sozinha quando o estoque responde atrasado. Uma nota expirada tem `processingId`
preenchido, `printing: false` e `editable: true`.

```
printing  = processingId != null && lastError == null
editable  = status == 'Open' && !printing
```

Os três estados que a tela precisa distinguir:

| Situação | `status` | `processingId` | `lastError` | `printing` | `editable` |
|---|---|---|---|---|---|
| Aberta, ociosa | `Open` | `null` | `null` | `false` | `true` |
| Imprimindo | `Open` | preenchido | `null` | `true` | `false` |
| Rejeitada pelo estoque | `Open` | `null` | motivo | `false` | `true` |
| Impressão expirada | `Open` | **preenchido** | motivo | `false` | `true` |
| Fechada | `Closed` | `null` | `null` | `false` | `false` |

### InvoiceItem

| Campo | Tipo | Nota |
|---|---|---|
| `id` | `number` | **BIGINT**, não UUID |
| `productId` | `string` | UUID |
| `productCode` | `string` | snapshot |
| `productDescription` | `string` | snapshot |
| `quantity` | `number` | `> 0` |

`AddInvoiceItemRequest` leva `productId` e `quantity`.
`UpdateInvoiceItemRequest` leva só `quantity`.

### PagedResult\<T\>

| Campo | Tipo |
|---|---|
| `items` | `T[]` |
| `page` | `number` |
| `size` | `number` |
| `total` | `number` |
| `totalPages` | `number` |

Parâmetros de consulta: `page` (padrão 1), `size` (padrão 20, teto 100).
Produtos aceitam `search`; notas aceitam `status`, que aceita tanto o nome (`?status=Open`)
quanto o número (`?status=1`). Valor inválido responde **400** `validation_error`.

### ProblemDetails

RFC 7807: `type?`, `title`, `status`, `detail?`, `instance?`, mais duas extensões que o backend
sempre envia: **`code`** (identificador estável do erro, é nele que o front decide) e
**`traceId`**. Em erro de validação vem também `errors` (dicionário campo → mensagens).

`title` e `detail` já vêm em português e podem ir direto para a tela.

## Contrato com a API

| Método | Rota | Retorno |
|---|---|---|
| `POST` | `/api/v1/auth/login` | 204 + `Set-Cookie` |
| `POST` | `/api/v1/auth/logout` | 204 |
| `GET` | `/api/v1/auth/me` | `{ name, email, roles }` |
| `GET` | `/api/v1/produtos?page&size&search` | `PagedResult<Product>` |
| `GET` | `/api/v1/produtos/{id}` | `Product` |
| `POST` | `/api/v1/produtos` | 201 `Product` |
| `PUT` | `/api/v1/produtos/{id}` | `Product` |
| `DELETE` | `/api/v1/produtos/{id}` | 204, exclusão lógica |
| `GET` | `/api/v1/notas?page&size&status` | `PagedResult<Invoice>` |
| `GET` | `/api/v1/notas/{id}` | `Invoice` |
| `POST` | `/api/v1/notas` | 201 `Invoice` — sem corpo na requisição |
| `DELETE` | `/api/v1/notas/{id}` | 204, exclusão lógica |
| `POST` | `/api/v1/notas/{id}/itens` | 201 `Invoice` |
| `PUT` | `/api/v1/notas/{id}/itens/{itemId}` | `Invoice` |
| `DELETE` | `/api/v1/notas/{id}/itens/{itemId}` | `Invoice` |
| `POST` | `/api/v1/notas/{id}/impressao` | **202** `Invoice` |

A versão fica numa constante única do front (`core/api.const.ts`), não espalhada pelos services.

Endpoints que alteram nota devolvem a nota inteira — evita segunda chamada para atualizar a tela.

### Escrita de produto exige perfil

`POST`, `PUT` e `DELETE` de produto pedem `Administrador` ou `Gerente`; quem não tem recebe
**403** `forbidden`. A tela de produtos deve esconder os botões de escrita para os demais perfis
— o `roles` de `/auth/me` já traz o que é preciso.

### Visibilidade de notas

`GET /notas` devolve só as notas do próprio usuário. `Administrador` e `Gerente` veem todas.
Nota de outro usuário responde **404**, nunca 403 — o front trata como inexistente.

## Códigos de erro

O front decide pelo `code`, não pelo `status` nem pelo texto.

| `code` | HTTP | Quando |
|---|---|---|
| `invalid_credentials` | 401 | login errado |
| `invalid_session` | 401 | sem sessão ou expirada → redireciona para login |
| `invalid_antiforgery_token` | 400 | falta o header `X-XSRF-TOKEN` |
| `forbidden` | 403 | perfil não permite |
| `service_unavailable` | 503 | circuit breaker aberto — "sua nota continua aberta" |
| `too_many_requests` | 429 | rate limit; usar o header `Retry-After` |
| `invoice_not_found` | 404 | inexistente **ou** de outro usuário |
| `invoice_item_not_found` | 404 | — |
| `invoice_already_closed` | 409 | tentou alterar ou reimprimir nota fechada |
| `invoice_already_printing` | 409 | já tem impressão em voo |
| `invoice_item_duplicated` | 409 | produto já na nota — orientar a editar a quantidade |
| `invoice_empty` | 422 | imprimir sem itens |
| `product_not_found` | 422 | produto ainda não replicado no faturamento |
| `product_inactive` | 422 | produto desabilitado |
| `product_code_in_use` | 409 | código de produto repetido |
| `validation_error` | 400 | `errors` traz campo → mensagens |

## Services

### ProductsService

| Método | Retorno |
|---|---|
| `list(params)` | `PagedResult<Product>` |
| `getById(id)` | `Product` |
| `create(req)` | `Product` |
| `update(id, req)` | `Product` |
| `remove(id)` | `void` |
| `search(term)` | `Product[]` — typeahead, mesmo endpoint com `size: 10` |

### InvoicesService

| Método | Retorno |
|---|---|
| `list(params)` | `PagedResult<Invoice>` |
| `getById(id)` | `Invoice` |
| `create()` | `Invoice` |
| `remove(id)` | `void` |
| `addItem(id, req)` | `Invoice` |
| `updateItem(id, itemId, req)` | `Invoice` |
| `removeItem(id, itemId)` | `Invoice` |
| `print(id)` | `Invoice` — 202 |
| `track(id)` | `Invoice` — emite a cada 1,5s **enquanto `printing` for `true`** |

`track` para quando `printing` vira `false` — não quando `processingId` vira `null`. Uma
impressão expirada nunca zera o `processingId`, então a condição antiga rodaria para sempre.

### AuthService

| Membro | Papel |
|---|---|
| `user` | signal somente leitura do usuário logado |
| `authenticated` | `computed` — usado pelo `authGuard` |
| `login(email, password)` | 204 + cookie |
| `logout()` | 204 |
| `loadSession()` | `GET /auth/me` |

`loadSession()` roda no `APP_INITIALIZER` — como o token está num cookie `HttpOnly`, o front não
tem como saber se há sessão sem perguntar ao servidor.

## Sessão e mesma origem

O cookie é `HttpOnly` + `SameSite=Strict`, então o navegador **precisa** enxergar front e API na
mesma origem. Duas topologias válidas:

| Modo | Origem única | Como |
|---|---|---|
| `ng serve` | `localhost:4200` | `proxy.conf.json` encaminhando `/api` para o gateway 🕓 ainda não criado |
| `docker compose --profile app` | `localhost:5000` | o gateway serve os estáticos e a API |

Bater direto em `localhost:5108` ou `:5247` não funciona: esses serviços só aceitam o JWT interno
que o gateway assina, e não conhecem o cookie.

O antiforgery exige `X-XSRF-TOKEN` em toda requisição insegura autenticada. O valor vem do cookie
`XSRF-TOKEN`, que o gateway publica em qualquer `GET`. `withXsrfConfiguration` do Angular faz
isso sozinho desde que a origem seja a mesma.
