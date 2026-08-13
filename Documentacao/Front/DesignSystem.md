# Design System — Front

Base gerada com a skill `ui-ux-pro-max`, filtrada para app interno.

**Aproveitado:** paleta, tipografia, guidelines de UX e checklist de acessibilidade.
**Descartado:** o estilo *Exaggerated Minimalism* e o padrão *Operations Landing* — a base da skill é de landing page, e este é um CRUD autenticado com tabela densa. Título de 12rem numa lista de notas fiscais é o oposto do que a tela precisa. GSAP também sai: não há página de rolagem para animar.

## Tokens — três camadas

`primitivo → semântico → componente`. Componente nunca referencia hex direto.

### Primitivo

| Token | Valor |
|---|---|
| `--slate-900` | `#0F172A` |
| `--slate-700` | `#334155` |
| `--slate-600` | `#475569` |
| `--slate-200` | `#E6E8EA` |
| `--slate-100` | `#F2F3F4` |
| `--slate-50` | `#F8FAFC` |
| `--green-600` | `#059669` |
| `--amber-500` | `#F59E0B` |
| `--red-600` | `#DC2626` |
| `--white` | `#FFFFFF` |

### Semântico

| Token | Primitivo |
|---|---|
| `--color-primary` | `--slate-700` |
| `--color-on-primary` | `--white` |
| `--color-secondary` | `--slate-600` |
| `--color-accent` | `--green-600` |
| `--color-background` | `--slate-50` |
| `--color-surface` | `--white` |
| `--color-foreground` | `--slate-900` |
| `--color-muted` | `--slate-100` |
| `--color-border` | `--slate-200` |
| `--color-success` | `--green-600` |
| `--color-warning` | `--amber-500` |
| `--color-destructive` | `--red-600` |
| `--color-ring` | `--slate-700` |

### Componente

| Token | Origem |
|---|---|
| `--chip-aberta-bg` / `-fg` | `--color-muted` / `--color-secondary` |
| `--chip-processando-bg` / `-fg` | `--color-primary` a 10% / `--color-primary` |
| `--chip-fechada-bg` / `-fg` | `--color-success` a 12% / `--color-success` |
| `--table-header-bg` | `--color-muted` |
| `--table-row-hover` | `--color-primary` a 4% |

*Slate industrial + verde de estoque* — neutro para leitura longa, com o verde reservado a confirmação e saldo positivo.

## Identidade por seção

Duas seções, dois contextos físicos. **Produtos é o chão de fábrica; Notas é o escritório.** A cor muda para o usuário saber onde está sem precisar ler.

| | Produtos | Notas Fiscais |
|---|---|---|
| Metáfora | almoxarifado, sinalização, empilhadeira | documento, papel, arquivo |
| Cor | laranja industrial | azul corporativo |
| Textura | faixa de risco diagonal | barra sólida fina |
| Canto | 4px — mais quadrado | 6px |
| Ritmo | denso, etiquetado | espaçado, documental |

A base slate permanece nas duas — muda só a camada de identidade:

| Token | Produtos | Notas | Uso |
|---|---|---|---|
| `--secao-accent` | `#EA580C` | `#2563EB` | barra, faixa, ícone — nunca texto corrido |
| `--secao-texto` | `#9A3412` | `#1D4ED8` | título e link |
| `--secao-forte` | `#C2410C` | `#1E40AF` | botão preenchido |
| `--secao-surface` | `#FFF7ED` | `#EFF6FF` | fundo de destaque |
| `--secao-border` | `#FED7AA` | `#BFDBFE` | borda |
| `--secao-radius` | `4px` | `6px` | canto |

**Por que dois laranjas e dois azuis.** `#EA580C` atinge 3:1, suficiente para elemento de interface (barra, ícone, borda) mas **não** para texto, que exige 4.5:1. Por isso `--secao-texto` é mais escuro. Usar o mesmo tom nos dois papéis é o erro clássico que derruba o contraste.

### Onde a cor de seção entra — e onde não

| Aplica | Não aplica |
|---|---|
| barra superior da seção | chip de status (Aberta / Fechada) |
| título da página | toast de sucesso, erro, aviso |
| link ativo na navegação | validação de formulário |
| botão primário da seção | badge de saldo baixo |
| sublinhado do cabeçalho da tabela | ícone destrutivo |

**Regra:** cor de seção é *identidade*, nunca *significado*. Verde é sempre sucesso, vermelho é sempre erro — nas duas seções. Se o significado mudasse por contexto, o usuário teria que aprender dois vocabulários.

Cuidado específico: o laranja de Produtos convive com o âmbar de aviso (`--color-warning`). Não colidem porque ocupam lugares distintos — o laranja é estrutural (topo, borda, navegação) e o âmbar é sempre chip preenchido com texto ao lado.

### Motivos visuais

| Seção | Motivo | Leitura |
|---|---|---|
| Produtos | faixa de risco diagonal de 6px no topo | sinalização de armazém |
| Produtos | código do produto em Fira Code sobre fundo slate, canto reto | etiqueta de prateleira |
| Produtos | cabeçalho de coluna em maiúsculas, `letter-spacing` largo | placa de armazém |
| Produtos | saldo grande e monoespaçado | leitor de balança |
| Notas | barra sólida de 4px no topo | cabeçalho de documento |
| Notas | nota renderizada como folha — superfície branca, borda superior azul, `max-width` 960px, margens generosas | papel, não planilha |

### Como a seção é definida

A rota carrega `data.secao`; o shell lê esse valor pelo router e o expõe como atributo `data-secao` no container raiz. Um atributo troca o tema inteiro por cascata de CSS — nenhum componente precisa saber em que seção está.

A transição entre seções usa 200ms `ease-in-out` na cor, então a mudança é percebida sem piscar.

## Tipografia

| Uso | Fonte | Peso |
|---|---|---|
| Títulos e números | **Fira Code** | 500 / 600 |
| Corpo, formulários | **Fira Sans** | 400 / 500 |

Fira Code em número de nota, código de produto e saldo — é monoespaçada, então as colunas alinham na tabela. Fira Sans no resto.

| Token | Tamanho | Line-height | Uso |
|---|---|---|---|
| `--text-xs` | 12px | 1.5 | legenda, helper |
| `--text-sm` | 14px | 1.5 | tabela, label |
| `--text-base` | 16px | 1.5 | corpo |
| `--text-lg` | 20px | 1.4 | título de card |
| `--text-xl` | 24px | 1.3 | título de página |

Nada abaixo de 12px. Corpo em 16px.

## Espaçamento e densidade

Escala de 4px: `4 · 8 · 12 · 16 · 24 · 32 · 48`, exposta como `--space-1` a `--space-12`. Raio padrão `6px`, `8px` no card.

Raio pequeno e sombra discreta. A skill lista **sombra complexa e efeito 3D como anti-padrão** — vale aqui.

## Tema Angular Material

Paletas M3 geradas a partir da cor semente com `ng generate @angular/material:m3-theme`.

| Slot | Valor |
|---|---|
| Primária | `#334155` |
| Terciária | `#059669` |
| `brand-family` | Fira Code |
| `plain-family` | Fira Sans |
| `density` | `-1` |

`density: -1` é o que traduz o app para uso profissional em desktop — o padrão do Material é folgado demais para tela com muitas linhas.

## Layout

```
┌──────────────────────────────────────────────────────────┐
│  ▣ Emissor    Produtos   Notas              Augusto ▾    │  mat-toolbar
├──────────────────────────────────────────────────────────┤
│                                                          │
│    ┌────────────────────────────────────────────────┐    │
│    │  conteúdo                                      │    │  max-width 1440
│    │                                                │    │  padding 24px
│    └────────────────────────────────────────────────┘    │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

## Responsividade

Mobile-first. Nenhuma largura fixa em px no container.

| Breakpoint | Faixa | Layout |
|---|---|---|
| `xs` | < 600px | coluna única · tabela vira card · toolbar com menu hambúrguer |
| `sm` | 600–959px | oculta colunas secundárias · dialog vira fullscreen |
| `md` | 960–1279px | tabela completa · container 100% |
| `lg` | ≥ 1280px | tabela completa · container `max-width: 1440px` |

### Colunas por breakpoint

| Tela | `xs` | `sm` | `md+` |
|---|---|---|---|
| Produtos | card: código + descrição + saldo | código, descrição, saldo | + ações |
| Notas | card: número + status | número, status, data | + emitente, ações |
| Itens da nota | card: descrição + qtd | código, descrição, qtd | + ações |

### Regras

| Recurso | Comportamento |
|---|---|
| Detecção | `BreakpointObserver` do CDK exposto como signal; o template troca `matColumnDef` pelo signal, sem duplicar tabela |
| Tabela em `xs` | vira lista de `mat-card`, uma por registro |
| Overflow | `overflow-x: auto` no wrapper da tabela — a página nunca rola na horizontal, só a tabela |
| Dialog em `xs` | ocupa a tela inteira; 520px em celular fica cortado |
| Toolbar em `xs` | links viram `MatSidenav` modo `over` |

Testar em **375, 768, 1024 e 1440px**.

## Componentes padronizados

### Tabela

```
┌─ cabeçalho fixo, fundo --table-header-bg, texto 14px 500 ─┐
│  Código      Descrição              Saldo        Ações    │
├───────────────────────────────────────────────────────────┤
│  P001        Caneta esferográfica     120          ⋯      │  hover: --table-row-hover
└───────────────────────────────────────────────────────────┘
```

| Regra | Valor |
|---|---|
| Altura da linha | 44px (mínimo de toque) |
| Alinhamento | texto à esquerda, número à direita |
| Fonte de número | Fira Code, tabular |
| Zebra | **não** — usa borda 1px `--color-border` |
| Ordenação | `MatSort` na coluna, indicador visível |
| Paginação | `MatPaginator`, 10 / 20 / 50 |

Ação por linha em `MatMenu` (`⋯`), não botões soltos — evita a fileira de ícones que polui a coluna.

### Estados de lista

Toda listagem tem os quatro:

| Estado | Componente |
|---|---|
| Carregando | `MatProgressBar` indeterminada no topo da tabela |
| Vazio | ícone + "Nenhum produto cadastrado" + botão de ação primária |
| Erro | ícone + mensagem + botão "Tentar novamente" |
| Com dados | tabela |

Nunca spinner cobrindo a tela inteira: a barra no topo mantém o contexto visível.

### Formulário

| Regra | Valor |
|---|---|
| Appearance | `outline` |
| Label | **sempre visível** — nunca só placeholder |
| Validação | no `blur`, não só no submit |
| Erro | `mat-error` embaixo do campo, nunca só no topo |
| Helper | `mat-hint` para formato e restrição |
| Botão submit | desabilitado enquanto inválido ou em voo |
| Largura | `max-width: 480px` em coluna única |

### Modal

`MatDialog` para criar, editar e confirmar. Nunca para exibir erro — isso é toast.

| Tipo | Largura | Uso |
|---|---|---|
| Formulário | 520px | cadastro e edição de produto |
| Confirmação | 400px | remover item, ação destrutiva |

Formulário abre com `disableClose` — exige Cancelar explícito —, foco no primeiro campo tabulável e restauração do foco ao fechar.

Estrutura fixa: `mat-dialog-title` → `mat-dialog-content` → `mat-dialog-actions align="end"` com **Cancelar à esquerda, ação primária à direita**.

Confirmação destrutiva usa botão `warn` e nomeia a ação — "Remover item", não "OK".

### Toast

`MatSnackBar` centralizado num `ToastService` com três níveis. Componente nunca chama o `MatSnackBar` direto.

| Tipo | Duração | Cor da borda |
|---|---|---|
| Sucesso | 4s | `--color-success` |
| Aviso | 5s | `--color-warning` |
| Erro | 7s | `--color-destructive` |

Posição inferior direita, ação "Fechar" sempre presente. Sempre auto-dismiss — a skill marca "toast que nunca some" como anti-padrão. Erro dura mais porque há texto para ler.

Erro de servidor chega pelo `erroInterceptor` e cai direto no `ToastService.erro()`.

### Chip de status

| Status | Fundo | Texto |
|---|---|---|
| Aberta | `--chip-aberta-bg` | `--chip-aberta-fg` |
| Processando | `--chip-processando-bg` | `--chip-processando-fg` |
| Fechada | `--chip-fechada-bg` | `--chip-fechada-fg` |

Cor **nunca sozinha** — o chip sempre traz o texto do status. Regra de acessibilidade: daltônico precisa distinguir sem depender do matiz.

### Botões

| Tipo | Uso |
|---|---|
| `mat-flat-button color="primary"` | ação primária, uma por tela |
| `mat-stroked-button` | secundária (Cancelar) |
| `mat-icon-button` | ação em linha de tabela |
| `mat-flat-button color="warn"` | destrutiva confirmada |

Mínimo 44×44px de área clicável. `cursor: pointer` em tudo que clica. Botão em requisição mostra spinner interno e fica desabilitado.

### Ícones

`MatIcon` com Material Symbols. **Nenhum emoji como ícone** — os `✎` e `🗑` dos wireframes em [Telas.md](Telas.md) são rascunho ASCII, viram `edit` e `delete`.

Ícone sozinho exige `aria-label`.

## Animações

Biblioteca: `@angular/animations`. Faixa de 150–300ms. Animar só `opacity` e `transform`.

| Transição | Duração | Easing |
|---|---|---|
| Hover, foco | 150ms | `ease-out` |
| Entrada de item na lista | 180ms | `ease-out` |
| Saída de item | 150ms | `ease-in` |
| Abrir modal | 200ms | `ease-out` |
| Toast entrando | 250ms | `ease-out` |
| Troca de rota | 200ms | `ease-in-out` |
| Stagger entre linhas | 40ms | — |

### Onde cada uma entra

| Animação | Tela |
|---|---|
| `cascata` | carga da tabela de produtos e de notas |
| `itemLista` | adicionar e remover item da nota |
| `entrada` | banner de erro, estado vazio |
| transição de chip | Aberta → Processando → Fechada |
| pulso no saldo | célula pisca quando o saldo muda após a impressão |

Entrada e saída de item usam `grid-template-rows: 0fr → 1fr` em vez de animar `height` — o navegador resolve sem reflow a cada frame e não exige saber a altura antecipadamente.

O pulso no saldo é o detalhe que rende no vídeo: depois de fechar a nota, a linha do produto pisca em verde com o valor novo, e fica visível que a baixa aconteceu.

### Movimento reduzido

`prefers-reduced-motion: reduce` zera animação e transição globalmente. O spinner da impressão é **exceção deliberada**: é feedback de estado, não decoração, e permanece.

## Scroll

| Recurso | Onde | Como |
|---|---|---|
| Cabeçalho fixo | tabelas | `matHeaderRowDef` com `sticky` |
| Scroll virtual | autocomplete de produto | `cdk-virtual-scroll-viewport` |
| Voltar ao topo | troca de rota | `withInMemoryScrolling` |
| Rolar até o erro | submit inválido | `scrollIntoView` suave no primeiro `mat-error` |
| Rolagem suave | global | `scroll-behavior: smooth` |

**O que não fazer:** revelar conteúdo conforme o usuário rola (*scroll reveal*). É padrão de landing page — numa tabela de notas, linha que aparece só quando entra na viewport atrapalha a leitura e quebra o Ctrl+F. A cascata acontece **uma vez**, na carga, não a cada rolagem.

## Checklist antes de gravar o vídeo

- [ ] Contraste de texto ≥ 4.5:1
- [ ] Foco visível no teclado — nunca `outline: none` sem substituto
- [ ] Todo input com `<label>` associado
- [ ] Ícone sem texto tem `aria-label`
- [ ] Nenhum emoji como ícone
- [ ] `cursor: pointer` em tudo clicável
- [ ] Alvo de toque ≥ 44×44px
- [ ] Tabela com `overflow-x: auto`
- [ ] Sem rolagem horizontal na página
- [ ] `prefers-reduced-motion` respeitado
- [ ] Estados vazio e erro implementados, não só o caminho feliz
- [ ] Testado em 1440px, 1024px, 768px e 375px
