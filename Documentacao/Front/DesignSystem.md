# Design System — Front

Um sistema, **dois dialetos**. Mesma gramática — grade de 8, semântica de cor, acessibilidade — e tudo o mais diferente entre Estoque e Faturamento.

Origem: duas consultas separadas à skill `ui-ux-pro-max`. `"warehouse inventory industrial signage"` devolveu **Kinetic Brutalism** (raio ~0, traço grosso, caixa alta, alto contraste). `"fiscal document ledger accounting formal"` devolveu **Flat Design** com o par **EB Garamond / Lato** — "legal, formal, autoritário, para contratos e documentos". O serifado do Faturamento vem daí, não é escolha arbitrária.

Implementação em [`NotaFlow/src/styles/_tokens.scss`](../../NotaFlow/src/styles/_tokens.scss).

---

## 1. A gramática compartilhada

### Grade de 8

```
--s-1: 8px    --s-2: 16px   --s-3: 24px   --s-4: 32px
--s-5: 40px   --s-6: 48px   --s-8: 64px   --s-10: 80px
```

`--s-half: 4px` é **exceção nomeada**, válida só em três lugares: raio de canto, vão entre ícone e rótulo, deslocamento de borda. Em qualquer outro lugar é erro — se aparecer `padding: 12px`, está fora do sistema.

A grade levou a altura de linha da tabela de 44px para **48px**: cai na grade e supera com folga o mínimo de toque de 44px.

### Tipografia

| Papel | Fonte | Onde |
|---|---|---|
| Corpo | **Fira Sans** | texto corrido, formulário, rótulo |
| Dado e placa | **Fira Code** | número, código, saldo, tudo no Estoque |
| Documento | **EB Garamond** | número da nota, títulos do Faturamento |

```
--t-xs: 12px   --t-sm: 14px   --t-base: 16px
--t-lg: 20px   --t-xl: 26px   --t-2xl: 38px
```

Nada abaixo de 12px. Corpo em 16px.

### Cor semântica — invariável

```
--ok  #047857   --warn  #B45309   --bad  #C81E1E     (claro)
--ok  #34D399   --warn  #FBBF24   --bad  #F87171     (escuro)
```

**Verde é sucesso e vermelho é erro nos dois dialetos.** Cor de dialeto diz *onde você está*; cor semântica diz *o que aconteceu*. Se o significado mudasse por seção, o usuário teria que aprender dois vocabulários.

Por isso o chip de status, o toast e a validação de formulário **nunca** recebem a cor do dialeto.

### Neutros

Cinza puxado para o azul — escolhido, não herdado. `--bg`, `--surface`, `--surface-2`, `--surface-3`, `--fg`, `--fg-muted`, `--fg-subtle`, `--line`, `--line-hard`.

### Movimento

```
--dur-1: 100ms   --dur-2: 180ms   --dur-3: 260ms
--ease-out: cubic-bezier(.16, 1, .3, 1)
```

Animar só `opacity` e `transform`. `prefers-reduced-motion` zera tudo — exceto o spinner da impressão, que é estado, não decoração.

---

## 2. Os dois dialetos

Um atributo no container raiz troca a identidade inteira por cascata. **Nenhum componente sabe em que seção está.**

```html
<div class="casca" [attr.data-dial]="dialeto()">
```

```ts
{ path: 'produtos', data: { dialeto: 'estoque' } }
{ path: 'notas',    data: { dialeto: 'faturamento' } }
```

### Token por token

| Token | Estoque | Faturamento | Por quê |
|---|---|---|---|
| `--d-display` | Fira Code | EB Garamond | placa de armazém × documento formal |
| `--d-radius` | `2px` | `10px` | metal × papel |
| `--d-bw` | `1px` | `1px` | fio fino no contorno geral |
| `--d-bw-strong` | `2px` | `1px` | só o item ativo engrossa |
| `--d-case` | `uppercase` | `none` | sinalização × leitura corrida |
| `--d-track` | `.09em` | `0` | caixa alta exige respiro |
| `--d-speed` | `100ms` | `200ms` | fábrica é seca; escritório é suave |
| `--d-paper` | branco frio | `#FEFDFA` morno | papel real não é branco puro |
| `--d-lift` | `3px 3px 0` **sem desfoque** | sombra macia, 2 camadas | chapa estampada × papel na mesa |
| `--d-accent` | `#EA580C` | `#2563EB` | laranja de segurança × azul de carimbo |
| `--d-ink` | `#9A3412` | `#1D4ED8` | tom de texto (4.5:1) |
| `--d-solid` | `#C2410C` | `#1E40AF` | botão preenchido |

**Dois laranjas e dois azuis de propósito.** `#EA580C` atinge 3:1 — suficiente para elemento de interface (barra, ícone, borda), **insuficiente para texto**, que exige 4.5:1. Por isso `--d-ink` é mais escuro. Usar o mesmo tom nos dois papéis é o erro clássico que derruba o contraste.

### Motivos visuais

| Estoque | Faturamento |
|---|---|
| faixa de risco diagonal no topo | barra sólida fina, pautado discreto |
| código em bloco escuro, canto reto — etiqueta de prateleira | número em serifada corpo 20 |
| cabeçalho em caixa alta, tracking largo | fio vertical entre colunas |
| saldo grande e monoespaçado | total fechado em **linha dupla** |
| status **estampado**: retângulo, borda 2px | status como **carimbo**: rotacionado 3°, contorno fino |

### O relevo, e por que não é neumorfismo

A skill classifica neumorfismo como **"Light-only · Dark quebra a metáfora do material · risco de baixo contraste"**. Este app tem tema escuro.

Adotamos a variante que ela recomenda, **Soft UI Evolution**: *"sombras mais suaves que flat, mais claras que neumorfismo, WCAG AA+, claro e escuro completos"*. Mesma sensação tátil, contraste que passa — e o relevo virou material de dialeto em vez do mesmo plástico cinza nos dois.

---

## 3. Tema claro e escuro

Três estados, não dois:

| Estado | Como | Regra CSS |
|---|---|---|
| Sistema (padrão) | sem atributo | `@media (prefers-color-scheme: dark)` guardado por `:root:not([data-theme='light'])` |
| Claro explícito | `data-theme="light"` | vence o sistema escuro |
| Escuro explícito | `data-theme="dark"` | vence o sistema claro |

**Nenhuma cor pode ter sua única definição dentro de um bloco de media ou `[data-theme]`.** O `:root` puro define a paleta clara completa; os outros dois blocos só redefinem tokens. Cor definida apenas atrás de `[data-theme]` nunca se aplica no estado sem atributo — é o bug clássico de artefato ilegível.

`ThemeService` guarda a escolha em `localStorage` e aplica o atributo no boot.

---

## 4. Layout e responsividade

**Mobile primeiro.** Todo `@media` é `min-width`, nunca `max-width`.

| Faixa | Navegação | Tabela | Modal |
|---|---|---|---|
| `< 768px` | barra inferior, **4 destinos** | vira lista de cartões | tela cheia |
| `768–1023px` | trilho de 64px, só ícone | completa | 520px |
| `≥ 1024px` | lateral 236px expandida | completa | 520px |

A lateral **não vira gaveta escondida** no mobile: gaveta custa um toque extra em toda navegação. Vira barra inferior, ao alcance do polegar. Com 4 destinos, cada alvo fica com 25% da largura — acima de 44px até em 320px.

Testar em **375, 768, 1024 e 1440px**.

---

## 5. Componentes

### Barra lateral

Recolhível: 236px ↔ 64px. Grupos rotulados, medalha de contagem, divisor, "Sair" ancorado no rodapé. Ao recolher, rótulo/medalha/grupo somem e os ícones centralizam.

Item ativo marcado por **três sinais**, nunca só cor: no Estoque bloco preenchido invertido com borda de 2px; no Faturamento barra lateral de 3px + fundo suave + peso 600.

### Tabela

| Regra | Valor |
|---|---|
| Altura da linha | 48px |
| Alinhamento | texto à esquerda, número à direita |
| Fonte de número | Fira Code, tabular |
| Zebra | **não** — separação por borda de 1px |
| Cabeçalho | `sticky` |
| Overflow | `overflow-x: auto` no wrapper; a página nunca rola na horizontal |
| Paginação | 10 / 20 / 50 |

Ação por linha em menu `⋯`, não fileira de ícones.

### Estados de lista

Toda listagem tem os quatro: **carregando** (esqueleto, ver §6), **vazio** (`<app-empty-state>`), **erro** (mensagem + "Tentar novamente"), **com dados**.

Nunca spinner cobrindo a tela — perde o contexto.

### Botões

| Tipo | Uso |
|---|---|
| primário | ação principal, **uma por tela** |
| secundário | Cancelar |
| destrutivo | ação confirmada, rótulo nomeia a ação |
| ícone | ação em linha de tabela, exige `aria-label` |

Altura 48px. Botão em requisição **mantém a largura**, troca o conteúdo por spinner e desabilita — o layout não pode saltar. Ao pressionar, o relevo afunda 2px.

### Formulário

`outline`, rótulo **sempre visível** (nunca só placeholder), validação no `blur`, erro embaixo do campo, submit desabilitado enquanto inválido ou em voo, `max-width: 480px` em coluna única.

### Modal

Criar, editar e confirmar. **Nunca para exibir erro** — isso é toast.

520px para formulário, 400px para confirmação, tela cheia abaixo de 600px. Abre com `disableClose`, foco no primeiro campo, foco devolvido ao fechar. **Cancelar à esquerda, ação primária à direita.** Confirmação destrutiva nomeia a ação: "Remover item", não "OK".

### Toast

Centralizado num `ToastService`; componente nunca chama o snackbar direto.

| Tipo | Duração |
|---|---|
| Sucesso | 4s |
| Aviso | 5s |
| Erro | 7s — há texto para ler |

Canto inferior direito, sempre com auto-dismiss. **Toast que nunca some é anti-padrão.**

### Chip de status

Cor **nunca sozinha** — o chip sempre traz o texto. Daltônico precisa distinguir sem depender do matiz.

### Toggle de notificação

`role="switch"` com `aria-checked`, alvo de 40×24 dentro de linha de 48px, rótulo textual sempre visível. **Switch aplica na hora** — checkbox pertence a formulário com submit; misturar os dois ensina o usuário a duvidar se salvou. Se o servidor recusar, volta ao estado anterior e sobe toast de erro.

Retangular no Estoque, arredondado no Faturamento.

### Ícones

Material Symbols em SVG. **Nenhum emoji como ícone.** Ícone sozinho exige `aria-label`.

---

## 6. Esqueleto de carga

**O esqueleto não é um componente à parte. É o componente real com dados de espera e o texto mascarado.**

```html
<tbody [appSkeleton]="carregando()">
  @for (nota of carregando() ? espera : notas(); track nota.id) { … }
</tbody>
```

É impossível divergir da exibição: mesma marcação, mesmas colunas, mesma altura de linha. Quando a tela muda, o esqueleto muda junto — não existe segunda cópia para esquecer de atualizar, e **não há salto de layout** quando o dado chega.

`color: transparent` mantém a caixa do texto, então a geometria é exatamente a real. Ícone e imagem apenas somem, com o espaço reservado.

**Convenção que isso exige:** texto sempre dentro de elemento em linha (`<span>`, `<b>`, `<small>`). É ele que vira a barra.

Implementação: [`_esqueleto.scss`](../../NotaFlow/src/styles/_esqueleto.scss) + diretiva [`Skeleton`](../../NotaFlow/src/app/shared/skeleton/skeleton.ts).

---

## 7. Onboarding

Quatro camadas, nenhuma dependente de alguém ler documentação.

### Tela de entrada

Credenciais de teste **na tela**, com botão que preenche o formulário. Ninguém deve caçar senha no README.

### Tour de primeira visita

`TourService` + `<app-coachmark>`. Quatro balões, uma vez só, guardado em `localStorage`. Sempre com **Próximo, Voltar e Pular** — tour que prende o usuário é anti-padrão. Item "Rever o tour" nos ajustes chama `forget()`.

O alvo é destacado por seletor CSS (`.tour-alvo`), com recorte de luz e rolagem até ele. No mobile o balão ancora no rodapé.

### Estado vazio que ensina

Não diz "sem resultados". Diz **por que aquilo importa** e oferece a ação:

> **Nenhum produto ainda**
> Cadastre o primeiro produto para conseguir emitir uma nota fiscal.
> `[ Cadastrar produto ]`

É o melhor tutorial que existe: aparece exatamente onde a pessoa travou, sem interromper quem já sabe.

### Modo Explorar

Botão no cabeçalho liga o modo; todo elemento anotado ganha contorno tracejado e um `?`, e revela a explicação ao clicar.

```html
<button [appExplain]="'Publica um comando para o estoque. A nota fica
                       Processando até o resultado voltar pela fila.'">
  Imprimir
</button>
```

A anotação mora **junto do elemento que explica** — não há arquivo de textos para dessincronizar. Funciona por teclado; `role="button"` só quando o modo está ligado.

### Vocabulário

Na interface não existe "saga", "outbox" nem "circuit breaker". Existe *"a nota fecha sozinha"*, *"nenhum saldo mudou"*, *"o estoque não respondeu"*.

Toda mensagem de erro responde três coisas: **o que houve**, **o que aconteceu com o trabalho do usuário**, **o que fazer agora**.

---

## 8. Telas de erro

A metáfora do dialeto continua valendo quando algo quebra.

| Código | Título | Dialeto |
|---|---|---|
| 404 | "Prateleira vazia" — prateleira com vão marcado | Estoque |
| 404 | "Nota fora do arquivo" — documento com selo de recusa | Faturamento |
| 403 | "Seu perfil não alcança esta tela" — cadeado | do contexto |
| 500 | "Máquina parada" + `traceId` visível para chamado | do contexto |
| 503 | "O estoque não respondeu — **sua nota continua aberta**, nenhum saldo foi debitado" | do contexto |
| offline | "A tela volta sozinha quando a conexão retornar" | do contexto |

Erro fora das seções usa o dialeto neutro. Toda tela nomeia o que houve, diz o que fazer e oferece saída — **nunca só um número**.

---

## 9. Animação

**Uma biblioteca: `anime.js` v4.** 10KB, agnóstica de framework. `animate()` + `stagger()` resolve os dois casos que CSS não faz bem: cascata da tabela e pulso do saldo.

```ts
import { animate, stagger } from 'animejs';

animate('.linha', { opacity: [0, 1], y: [8, 0], duration: 280, ease: 'outExpo', delay: stagger(40) });
```

**Motion, uiverse e React Bits são referência visual, não dependência.** O melhor da Motion (`AnimatePresence`, animação de layout) é da camada React e não traduz para Angular; React Bits é React puro. Duas bibliotecas para o mesmo problema é peso sem retorno.

O resto fica em `@angular/animations`, que amarra entrada e saída ao ciclo de vida do componente.

| Transição | Duração |
|---|---|
| hover, foco | 150ms |
| entrada de item | 180ms |
| abertura de modal | 200ms |
| troca de dialeto | 200ms |
| toast entrando | 250ms |
| cascata entre linhas | 40ms de atraso |

Entrada e saída de item usam `grid-template-rows: 0fr → 1fr` em vez de animar `height` — sem reflow por frame e sem precisar saber a altura.

**Não fazer:** revelar conteúdo conforme a rolagem. É padrão de landing page; em tabela atrapalha a leitura e quebra o Ctrl+F. A cascata acontece **uma vez**, na carga.

---

## 10. Checklist antes de gravar

- [ ] Contraste de texto ≥ 4.5:1 **nos dois temas**
- [ ] Foco visível no teclado — nunca `outline: none` sem substituto
- [ ] Todo input com `<label>` associado
- [ ] Ícone sem texto tem `aria-label`
- [ ] Nenhum emoji como ícone
- [ ] `cursor: pointer` em tudo clicável
- [ ] Alvo de toque ≥ 44px — a grade de 8 entrega 48
- [ ] Tabela com `overflow-x: auto`, página sem rolagem horizontal
- [ ] `prefers-reduced-motion` respeitado
- [ ] Estados vazio, carregando e erro implementados — não só o caminho feliz
- [ ] Esqueleto sem salto de layout ao carregar
- [ ] Testado em 1440, 1024, 768 e 375px

---

## Arquivos

| Caminho | O que é |
|---|---|
| `src/styles/_tokens.scss` | grade, tipografia, neutros, semântica, os dois dialetos |
| `src/styles/_esqueleto.scss` | máscara de carga |
| `src/styles/_explorar.scss` | modo explorar e destaque do tour |
| `src/app/core/services/theme.service.ts` | claro / escuro / sistema |
| `src/app/core/services/tour.service.ts` | passos, progresso, memória |
| `src/app/core/services/explore.service.ts` | liga o modo explorar |
| `src/app/shared/coachmark/` | balão do tour |
| `src/app/shared/empty-state/` | estado vazio que ensina |
| `src/app/shared/explain/` | diretiva `[appExplain]` |
| `src/app/shared/skeleton/` | diretiva `[appSkeleton]` |
