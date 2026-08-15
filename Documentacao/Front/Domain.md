# Domínio — Front

Models TypeScript e contrato com a API. Tudo passa pelo gateway em `/api/v1`.

## Models

### Produto

| Campo | Tipo | Nota |
|---|---|---|
| `id` | `string` | UUID |
| `code` | `string` | — |
| `description` | `string` | — |
| `balance` | `number` | inteiro `>= 0` |
| `active` | `boolean` | — |

`CreateProductRequest` leva `code`, `description` e `balance` — o saldo é o inicial.
`UpdateProductRequest` leva `code`, `description` e `active`: saldo não muda por edição de
cadastro, só pela baixa da nota.

### NotaFiscal

| Campo | Tipo | Nota |
|---|---|---|
| `id` | `string` | UUID |
| `numero` | `number` | gerado pelo backend |
| `status` | `'Aberta' \| 'Fechada'` | union type |
| `emitidaPorUsuarioNome` | `string` | snapshot |
| `criadaEm` | `string` | ISO |
| `fechadaEm` | `string \| null` | — |
| `processamentoId` | `string \| null` | `!= null` → imprimindo |
| `ultimoErro` | `string \| null` | motivo da rejeição |
| `itens` | `ItemNota[]` | — |

`status` chega como string porque o `SerializationConfig` do backend serializa enum como string. Sem isso viria `1` / `2` e o tipo aqui teria que ser numérico.

### ItemNota

| Campo | Tipo | Nota |
|---|---|---|
| `id` | `string` | UUID |
| `produtoId` | `string` | — |
| `produtoCodigo` | `string` | snapshot |
| `produtoDescricao` | `string` | snapshot |
| `quantidade` | `number` | `> 0` |

`ItemNotaRequest` leva só `produtoId` e `quantidade`.

### PaginaDe\<T\>

| Campo | Tipo |
|---|---|
| `items` | `T[]` |
| `page` | `number` |
| `size` | `number` |
| `total` | `number` |
| `totalPages` | `number` |

Parâmetros de consulta de produtos: `page` (padrão 1), `size` (padrão 20, teto 100), `search`.

### ProblemDetails

RFC 7807, espelhando o que o backend devolve: `type?`, `title`, `status`, `detail?`, `instance?`.

## Contrato com a API

| Método | Rota | Retorno |
|---|---|---|
| `POST` | `/api/v1/auth/login` | 204 + `Set-Cookie` |
| `POST` | `/api/v1/auth/logout` | 204 |
| `GET` | `/api/v1/auth/me` | `{ nome, email, perfis }` |
| `GET` | `/api/v1/produtos?page&size&search` | `PaginaDe<Produto>` |
| `GET` | `/api/v1/produtos/{id}` | `Produto` |
| `POST` | `/api/v1/produtos` | `Produto` |
| `PUT` | `/api/v1/produtos/{id}` | `Produto` |
| `DELETE` | `/api/v1/produtos/{id}` | 204, exclusão lógica |
| `GET` | `/api/v1/notas?pagina&tamanho&status` | `PaginaDe<NotaFiscal>` |
| `GET` | `/api/v1/notas/{id}` | `NotaFiscal` |
| `POST` | `/api/v1/notas` | `NotaFiscal` |
| `POST` | `/api/v1/notas/{id}/itens` | `NotaFiscal` |
| `DELETE` | `/api/v1/notas/{id}/itens/{itemId}` | `NotaFiscal` |
| `POST` | `/api/v1/notas/{id}/imprimir` | **202** `NotaFiscal` |

A versão fica numa constante única do front (`core/api.const.ts`), não espalhada pelos services.

Endpoints que alteram nota devolvem a nota inteira — evita segunda chamada para atualizar a tela.

## Services

### ProdutosService

| Método | Retorno |
|---|---|
| `listar(params)` | `PaginaDe<Produto>` |
| `obter(id)` | `Produto` |
| `criar(req)` | `Produto` |
| `atualizar(id, req)` | `Produto` |
| `buscar(termo)` | `Produto[]` — typeahead, mesmo endpoint com `size: 10` |

### NotasService

| Método | Retorno |
|---|---|
| `listar(params)` | `PaginaDe<NotaFiscal>` |
| `obter(id)` | `NotaFiscal` |
| `criar()` | `NotaFiscal` |
| `adicionarItem(id, req)` | `NotaFiscal` |
| `removerItem(id, itemId)` | `NotaFiscal` |
| `imprimir(id)` | `NotaFiscal` — 202 |
| `acompanhar(id)` | `NotaFiscal` — emite a cada 1,5s até `processamentoId` voltar a `null` |

### AuthService

| Membro | Papel |
|---|---|
| `usuario` | signal somente leitura do usuário logado |
| `autenticado` | `computed` — usado pelo `authGuard` |
| `login(email, senha)` | 204 + cookie |
| `logout()` | 204 |
| `carregarSessao()` | `GET /auth/me` |

`carregarSessao()` roda no `APP_INITIALIZER` — como o token está num cookie `HttpOnly`, o front não tem como saber se há sessão sem perguntar ao servidor.
