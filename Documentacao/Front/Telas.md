# Telas

Quatro telas. Todas com estados de carregando, vazio e erro.

## Login

```
        ┌──────────────────────────────────────┐
        │                                      │
        │       Emissão de Notas Fiscais       │
        │                                      │
        │   E-mail                             │
        │   ┌────────────────────────────────┐ │
        │   └────────────────────────────────┘ │
        │   Senha                              │
        │   ┌────────────────────────────────┐ │
        │   └────────────────────────────────┘ │
        │                                      │
        │   ┌────────────────────────────────┐ │
        │   │             Entrar             │ │
        │   └────────────────────────────────┘ │
        └──────────────────────────────────────┘
```

Reactive Form tipado. Botão desabilitado enquanto `form.invalid` ou requisição em voo.

| Estado | Comportamento |
|---|---|
| Enviando | botão vira spinner |
| 401 | mensagem genérica "e-mail ou senha inválidos" — não revelar qual |
| Sucesso | redireciona para `/produtos` |

## Produtos

```
┌─ Produtos ─────────────────────────────── [ + Novo produto ] ─┐
│                                                               │
│  ┌─ Buscar ───────────────────────────┐                       │
│  └────────────────────────────────────┘                       │
│                                                               │
│  Código     Descrição                    Saldo      Ações     │
│  ───────────────────────────────────────────────────────────  │
│  P001       Caneta esferográfica          120        ✎        │
│  P002       Caderno 96 folhas              45        ✎        │
│  P003       Borracha branca                 8        ✎        │
│                                                               │
│                              ◄ 1 2 3 ►    10 por página       │
└───────────────────────────────────────────────────────────────┘
```

`MatTable` + `MatSort` + `MatPaginator`. Busca com `debounceTime(300)`.

| Estado | Comportamento |
|---|---|
| Carregando | `MatProgressBar` no topo da tabela |
| Vazio | "Nenhum produto cadastrado" + atalho para cadastrar |
| Erro | snackbar + botão "tentar novamente" |

### Dialog de cadastro/edição

```
        ┌─ Novo produto ───────────────────────┐
        │                                      │
        │   Código                             │
        │   ┌────────────────────────────────┐ │
        │   └────────────────────────────────┘ │
        │   Descrição                          │
        │   ┌────────────────────────────────┐ │
        │   └────────────────────────────────┘ │
        │   Saldo                              │
        │   ┌──────────┐                       │
        │   └──────────┘                       │
        │                                      │
        │              [ Cancelar ]  [ Salvar ]│
        └──────────────────────────────────────┘
```

Os três campos são obrigatórios (requisito do PDF). `saldo` inteiro e `>= 0`. Foco no campo Código via `ngAfterViewInit`.

## Notas

```
┌─ Notas Fiscais ──────────────────────────── [ + Nova nota ] ─┐
│                                                              │
│  Número    Status        Emitida por        Data      Ações  │
│  ──────────────────────────────────────────────────────────  │
│  1042      ● Aberta      Augusto Santos     13/08     ▸      │
│  1041      ● Fechada     Augusto Santos     13/08     ▸      │
│  1040      ● Fechada     Maria Lima         12/08     ▸      │
│                                                              │
│                             ◄ 1 2 ►     10 por página        │
└──────────────────────────────────────────────────────────────┘
```

Status como `MatChip`: Aberta cinza, Fechada verde. Clique na linha abre o detalhe.

"Nova nota" cria com `POST /api/v1/notas` e navega direto para `/notas/{id}` — número e status Aberta vêm do backend.

## Nota — detalhe

A tela principal. É aqui que mora o requisito de impressão.

### Estado editável

```
┌─ Nota Fiscal nº 1042 ──────── [ Aberta ] ──── [ 🖨 Imprimir ] ─┐
│  Emitida por Augusto Santos · 13/08/2026 09:14                 │
│                                                                │
│  ┌─ Adicionar produto ────────────────────────────────────┐    │
│  │  [ buscar produto...              ▾ ]  [ qtd ]  [ + ]  │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                │
│  Código    Descrição                      Qtd       Ações      │
│  ────────────────────────────────────────────────────────────  │
│  P001      Caneta esferográfica            2         🗑         │
│  P003      Borracha branca                 1         🗑         │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

`MatAutocomplete` no campo de produto, alimentado por typeahead. Mostra código, descrição e saldo disponível na opção.

### Processando

```
┌─ Nota Fiscal nº 1042 ──── [ Processando ] ──── (  ⟳  ) ───────┐
│  Emitida por Augusto Santos · 13/08/2026 09:14                 │
│                                                                │
│  ⓘ Processando impressão...                                   │
│                                                                │
│  Código    Descrição                      Qtd                  │
│  ────────────────────────────────────────────────────────────  │
│  P001      Caneta esferográfica            2                   │
│  P003      Borracha branca                 1                   │
└────────────────────────────────────────────────────────────────┘
```

Botão vira spinner. Adicionar e remover item ficam bloqueados.

### Fechada

```
┌─ Nota Fiscal nº 1042 ──────── [ Fechada ] ──── [ 🖨 Imprimir ] ┐
│  Emitida por Augusto Santos · 13/08/2026 09:14                 │
│  Fechada em 13/08/2026 09:16                                   │
│                                        ↑ botão desabilitado    │
```

Botão desabilitado com tooltip "nota já fechada". Tabela somente leitura.

### Rejeitada

```
┌─ Nota Fiscal nº 1042 ──────── [ Aberta ] ──── [ 🖨 Imprimir ] ─┐
│  ⚠ Saldo insuficiente para o produto P003                      │
│                                        ↑ botão volta habilitado│
```

Banner com o `lastError` + snackbar. A nota continua editável para o usuário ajustar quantidade e tentar de novo.

### Impressão expirada

```
┌─ Nota Fiscal nº 1042 ──────── [ Aberta ] ──── [ 🖨 Imprimir ] ─┐
│  ⚠ O estoque não respondeu dentro do tempo esperado.           │
│    Tente novamente.                     ↑ botão volta habilitado│
```

Visualmente igual a Rejeitada — muda só o texto, que vem do mesmo `lastError`. A nota volta a
ser editável e o botão volta a funcionar. **A tela não precisa distinguir os dois casos.**

## Máquina de estados do botão Imprimir

O backend calcula `printing` e `editable` e os envia na nota. **A tela usa esses dois campos, não
deduz nada de `processingId`.**

| Estado | Condição | Botão | Chip |
|---|---|---|---|
| Editável | `editable = true` e `lastError = null` | habilitado | Aberta · cinza |
| Processando | `printing = true` | spinner, desabilitado | Processando · azul |
| Fechada | `status = 'Closed'` | desabilitado + tooltip | Fechada · verde |
| Rejeitada ou expirada | `editable = true` e `lastError != null` | habilitado | Aberta · cinza + banner |

A habilitação é um `computed`: nota carregada, `editable = true` e pelo menos um item. Nota sem
item também não imprime — não faz sentido dar baixa de nada.

> **Não use `processingId != null` para decidir "imprimindo".** Quando uma impressão expira, o
> backend preenche o `lastError` mas **preserva** o `processingId` — é isso que permite a nota se
> curar sozinha quando o estoque responde atrasado. Uma nota expirada tem `processingId`
> preenchido e mesmo assim está editável. Ver `Domain.md` para a tabela dos cinco estados.

O requisito do PDF diz "não permitir a impressão de notas com status diferente de Aberta". A tela **impede antes de chamar**; o `409` do backend é a segunda linha de defesa, não a primeira.

## Sequência da impressão

```
1. clique          → POST /notas/{id}/impressao
2. 202 Accepted    → nota volta com printing = true
3. spinner ligado  → inicia track(id), polling de 1,5s
4. cada tick       → GET /notas/{id}
5. printing = false  → polling encerra
      status Closed    → chip verde  + snackbar "Nota fechada com sucesso"
      lastError != null → banner     + snackbar vermelho
```

O passo 5 encerra por `printing = false`, **não** por `processingId = null` — numa impressão
expirada o `processingId` nunca zera e o polling rodaria para sempre.

Se o Estoque estiver fora, a nota fica em Processando até o `PrintExpirationWorker` marcar o
`lastError` (60s de tolerância, ciclo de 15s) ou até o serviço voltar e a fila drenar. Nos dois
casos o polling capta a mudança sem o usuário recarregar a página. Se o serviço voltar depois da
expiração, a nota **fecha sozinha** — o resultado atrasado ainda casa com o `processingId`
preservado.

Se o circuit breaker do gateway estiver aberto, o passo 1 responde **503** `service_unavailable`
de imediato, em vez de pendurar. A nota nem chega a entrar em Processando; o front mostra o
snackbar e mantém a tela editável.

**É esse o cenário de falha para gravar no vídeo:** derrubar o container do Estoque, clicar em
imprimir, mostrar o spinner e a fila enchendo no painel do RabbitMQ, subir o container, e a tela
fechar a nota sozinha.
