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

Banner com o `ultimoErro` + snackbar. A nota continua editável para o usuário ajustar quantidade e tentar de novo.

## Máquina de estados do botão Imprimir

| Estado | Condição | Botão | Chip |
|---|---|---|---|
| Editável | `status = 'Aberta'` e `processamentoId = null` | habilitado | Aberta · cinza |
| Processando | `processamentoId != null` | spinner, desabilitado | Processando · azul |
| Fechada | `status = 'Fechada'` | desabilitado + tooltip | Fechada · verde |
| Rejeitada | `status = 'Aberta'` e `ultimoErro != null` | habilitado | Aberta · cinza + banner |

A habilitação é um `computed`: nota carregada, `status = 'Aberta'`, `processamentoId = null` e pelo menos um item. Nota sem item também não imprime — não faz sentido dar baixa de nada.

O requisito do PDF diz "não permitir a impressão de notas com status diferente de Aberta". A tela **impede antes de chamar**; o `409` do backend é a segunda linha de defesa, não a primeira.

## Sequência da impressão

```
1. clique          → POST /notas/{id}/imprimir
2. 202 Accepted    → nota volta com processamentoId preenchido
3. spinner ligado  → inicia acompanhar(id), polling de 1,5s
4. cada tick       → GET /notas/{id}
5. processamentoId = null  → polling encerra
      status Fechada   → chip verde  + snackbar "Nota fechada com sucesso"
      ultimoErro != null → banner    + snackbar vermelho
```

Se o Estoque estiver fora, o passo 5 nunca chega — a nota fica em Processando até o `ImpressaoExpiradaWorker` do backend liberar (2 min) ou até o serviço voltar e a fila drenar. Nos dois casos o polling capta a mudança sem o usuário recarregar a página.

**É esse o cenário de falha para gravar no vídeo:** derrubar o container do Estoque, clicar em imprimir, mostrar o spinner e a fila enchendo no painel do RabbitMQ, subir o container, e a tela fechar a nota sozinha.
