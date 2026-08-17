# Emissor NF

Emissor de nota fiscal em microsserviços. Você cadastra um produto, abre uma nota, adiciona itens e manda imprimir — a impressão dá baixa no estoque. **Se faltar saldo em qualquer item, a nota inteira é recusada**: não existe baixa pela metade.

Teste técnico · Augusto Farias dos Santos

---

## Subir tudo

Um comando. Sobe os três serviços, os três bancos, a fila de mensagens e a interface.

```bash
docker compose --profile app up -d --build
```

Depois abra **http://localhost:5000**.

| | |
|---|---|
| **Usuário** | `admin@admin.com` |
| **Senha** | `Admin123!` |

As credenciais também aparecem na tela de login, com um botão que preenche o formulário e entra. Os bancos são criados e migrados sozinhos no primeiro boot — não há script para rodar à mão, e **nenhum dado de exemplo é inserido**: o sistema sobe vazio, com apenas a conta de administrador.

> A primeira subida leva alguns minutos porque o SQL Server demora a ficar pronto. O gateway espera por ele antes de aceitar requisição.

Para olhar por dentro:

| Endereço | O que é |
|---|---|
| `localhost:15672` | Painel do RabbitMQ (`Admin` / `Admin`) — dá para ver as mensagens passando |
| `localhost:5000/scalar/v1` | Documentação viva da API |

---

## O roteiro — 5 passos

Cada passo diz onde clicar e o que deve acontecer.

### 1 · Cadastre um produto · *Produtos*

Clique em **Novo produto**. Preencha código, descrição e saldo inicial **10**.

> **Deve acontecer:** o produto aparece na lista com saldo 10. Em segundo plano ele é replicado para o serviço de faturamento — por isso fica disponível na nota alguns instantes depois.

### 2 · Abra uma nota · *Notas fiscais*

Clique em **Nova nota**. Ela nasce com número sequencial e situação **Aberta**.

> **Deve acontecer:** a tela de detalhe abre direto, vazia, com a busca de produto pronta.

### 3 · Adicione um item · *Notas fiscais*

Digite duas letras do código, escolha o produto, quantidade **3**, confirme.

> **Deve acontecer:** o item entra com código e descrição copiados do catálogo. Tente adicionar o mesmo produto de novo — o sistema recusa e manda editar a quantidade do item existente.

### 4 · Imprima · *Notas fiscais*

Clique em **Imprimir**. A tela entra em **Processando** e acompanha sozinha.

> **Deve acontecer:** em 1 a 3 segundos a nota vira **Fechada**, sem recarregar a página. O botão de imprimir desabilita — nota fechada não reimprime.

### 5 · Confira a baixa · *Produtos*

> **Deve acontecer:** o saldo caiu de **10 para 7**. Os dois serviços têm bancos separados; o saldo mudou porque a impressão publicou uma mensagem que o estoque consumiu.

---

## Agora quebre de propósito

O caminho feliz é o fácil. Estes três cenários são o que o sistema tem de diferente.

### Tudo ou nada · 2 min

Crie um segundo produto com saldo **1**. Abra uma nota nova com **dois itens**: o primeiro produto com quantidade 2 (tem saldo) e o segundo com quantidade 99 (não tem). Mande imprimir.

A nota é **recusada inteira** e mostra o motivo. Volte em Produtos: **nenhum dos dois saldos mudou** — nem o que tinha saldo suficiente.

Corrija o segundo item para quantidade 1 e imprima de novo. Agora fecha, e os dois saldos caem juntos.

### Derrube o estoque no meio da impressão · 3 min

Este é o cenário que mostra por que existem microsserviços aqui.

```bash
docker compose --profile app stop estoque
```

Com o serviço fora, abra uma nota, adicione um item e mande imprimir. A nota entra em **Processando** e fica. Em cerca de um minuto avisa que o estoque não respondeu — mas **continua aberta e editável**, nada foi perdido.

```bash
docker compose --profile app start estoque
```

**Não toque em nada.** Quando o serviço volta, consome o comando que estava esperando na fila e a nota **fecha sozinha** na sua tela.

### Duas pessoas, dois escopos · 2 min

Em **Usuários**, crie uma conta nova com perfil Funcionário. Abra uma janela anônima e entre com ela.

A lista de notas vem **vazia** — ela não enxerga as notas do admin. Se tentar abrir pelo endereço direto, recebe "não encontrada". O admin, ao contrário, vê as notas de todo mundo. Ela também não consegue cadastrar produto: isso exige perfil Administrador ou Gerente.

---

## O que o app ensina sozinho

Nada acima depende de alguém ler este arquivo. As mesmas instruções aparecem dentro da aplicação, no momento em que fazem falta.

**Tela de entrada.** As credenciais de teste ficam na tela, com um botão que preenche o formulário e entra.

**Tour de primeira visita.** Nove passos que atravessam Painel, Produtos, Notas e Usuários — navegando entre as telas e destacando o elemento de cada explicação. Sempre com **Próximo**, **Voltar** e **Pular**. Para rever, use *Rever o tour* no pé da barra lateral.

**Modo Explorar.** Botão no topo. Com ele ligado, todo elemento importante ganha um `?`; clique para ver o que ele faz por trás — por exemplo, que *Imprimir* publica um comando numa fila e a nota fica Processando até o resultado voltar.

**Estados vazios que ensinam.** Não dizem "sem resultados". Dizem por que aquilo importa e oferecem a ação:

> **Nenhum produto ainda**
> Cadastre o primeiro produto para conseguir emitir uma nota fiscal.
> `[ Cadastrar produto ]`

**Erros que explicam.** Toda mensagem responde três coisas: o que houve, o que aconteceu com o seu trabalho, e o que fazer agora. *"Saldo insuficiente para P-0733. A nota não foi impressa e continua aberta. Ajuste a quantidade e tente de novo."*

---

## Como saber onde você está

O sistema tem duas áreas, e cada uma se veste de um jeito. É proposital: você reconhece o ambiente antes de ler o título.

| | Produtos | Notas fiscais |
|---|---|---|
| **Cor** | laranja | azul |
| **Ambiente** | almoxarifado, prateleira, etiqueta | escritório, documento, arquivo |
| **Letra** | monoespaçada, caixa alta | serifada, caixa mista |
| **Canto e traço** | 2px, borda grossa | 10px, fio fino |
| **O que você faz** | cadastra produto e confere saldo | emite nota e manda imprimir |

O que **não** muda: verde é sempre sucesso, vermelho é sempre erro, âmbar é sempre atenção — nas duas áreas. Cor de ambiente diz *onde*; cor de estado diz *o quê*.

A interface é mobile-first: abaixo de 768px a barra lateral vira barra inferior e as tabelas viram cartões.

---

## Arquitetura

```
                    ┌───────────────┐
   navegador ─────► │  API-Gateway  │  :5000   cookie de sessão, JWT interno,
   (cookie)         │     YARP      │          circuit breaker, serve o front
                    └───┬───────┬───┘
                        │       │
              ┌─────────┘       └─────────┐
              ▼                           ▼
     ┌─────────────────┐         ┌──────────────────┐
     │  API-Estoque    │         │  API-Faturamento │
     │  SQL Server     │         │  PostgreSQL      │
     │  dono do saldo  │         │  dono da nota    │
     └────────┬────────┘         └────────┬─────────┘
              │                           │
              └──────────┬────────────────┘
                         ▼
                    ┌──────────┐
                    │ RabbitMQ │   saga coreografada
                    └──────────┘
```

O navegador **só fala com o gateway**, sempre por cookie `HttpOnly`. O gateway troca o cookie por um JWT interno assinado antes de encaminhar — o front nunca vê token, os serviços nunca veem cookie.

### A impressão é uma saga

```
Faturamento  ──BaixarEstoqueCommand──►  Estoque
                                          │ debita tudo ou nada (savepoint)
                                          ▼
Faturamento  ◄──EstoqueBaixado / EstoqueRejeitado──┘
             fecha a nota          ou  recusa e reabre
```

Nenhuma chamada HTTP entre os dois serviços. Cada um grava o evento no próprio banco **na mesma transação** da mudança de estado (outbox transacional) e um worker publica depois. A entrega é *at-least-once*, então todo consumidor é idempotente.

| Decisão | Por quê |
|---|---|
| Tudo ou nada via savepoint | Uma rejeição desfaz os débitos parciais mas preserva o marcador de idempotência e o evento de recusa |
| Impressão expirada **mantém** a chave | É o que deixa a nota se curar sozinha quando o estoque responde atrasado |
| Comando duplicado **reemite** o resultado | Sem isso, um resultado perdido travaria a nota para sempre |
| Concorrência resolvida no banco | `UPDATE` condicional + `CHECK (balance >= 0)`, não lock de aplicação |
| A URN define a exchange do RabbitMQ | Por padrão o MassTransit usa o namespace CLR, e publicador e consumidor nunca se encontrariam |

---

## Rodar sem Docker

Infraestrutura em contêiner, serviços na IDE:

```bash
docker compose up -d          # só bancos + RabbitMQ
```

```bash
cd API-Gateway     && dotnet run --project Gateway          # :5000
cd API-Estoque     && dotnet run --project Estoque.Api      # :5247
cd API-Faturamento && dotnet run --project Faturamento.Api  # :5108
cd NotaFlow        && npm install && npm start              # :4200
```

Em desenvolvimento acesse **:4200** — o `proxy.conf.json` encaminha `/api` para o gateway, mantendo a mesma origem que o cookie `SameSite=Strict` exige. Bater direto em `:5247` ou `:5108` não funciona: esses serviços só aceitam o JWT interno que o gateway assina.

```bash
cd NotaFlow && npm test       # 18 testes de componente e de tour
```

---

## Estrutura

| Pasta | O que é | Stack |
|---|---|---|
| `API-Gateway/` | Entrada única: autenticação Argon2id, cookie, antiforgery, proxy YARP, circuit breaker | .NET 10 · PostgreSQL |
| `API-Estoque/` | Dono do saldo. Consome a baixa, publica o resultado | .NET 10 · Clean Architecture · SQL Server |
| `API-Faturamento/` | Dono da nota. Publica a baixa, consome o resultado | .NET 10 · Clean Architecture · PostgreSQL |
| `NotaFlow/` | Interface | Angular 21 · signals · `httpResource` |
| `Documentacao/` | Decisões de arquitetura, domínio e design system | — |

Cada serviço .NET tem sua própria solução `.slnx`.

---

## Se algo não funcionar

```bash
docker compose --profile app ps       # os 8 contêineres estão de pé?
docker compose --profile app logs -f gateway
```

O SQL Server é o mais lento a ficar pronto no primeiro boot; se uma tela mostrar *"não foi possível carregar"* logo após subir, o botão **Tentar novamente** resolve. Toda resposta de erro da API traz um `traceId` que permite achar a requisição exata no log do serviço.

Para começar do zero, apagando todos os dados:

```bash
docker compose --profile app down -v
docker compose --profile app up -d --build
```
