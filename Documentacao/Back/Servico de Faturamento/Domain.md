# Domínio — Serviço de Faturamento

**PostgreSQL** · notas fiscais, itens e ciclo de emissão. Não guarda saldo.

Sem `DEFAULT` e sem `CHECK` no banco: valores iniciais, numeração e validação de faixa são
responsabilidade do código.

```sql

CREATE SEQUENCE seq_nota_fiscal_numero;

```

## Tax_Invoice

```sql

CREATE TABLE Tax_Invoice (
    Id                      BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    Number                  BIGINT       NOT NULL,   -- nextval(seq_nota_fiscal_numero), pedido pelo código
    Status                  SMALLINT     NOT NULL,   -- 1 Aberta, 2 Fechada, 3 Com erro
    Issued_by_user_id       BIGINT       NOT NULL,   -- claim sub do JWT, sem FK
    Issued_by_user_name     VARCHAR(150) NOT NULL,   -- snapshot do claim name
    Created_at              TIMESTAMPTZ  NOT NULL,
    Closed_at               TIMESTAMPTZ  NULL,
    Processing_id           UUID         NULL,       -- impressão em voo / idempotência
    Processing_started_at   TIMESTAMPTZ  NULL,
    Last_error              TEXT         NULL
);

```

O snapshot do emitente existe para a nota continuar exibindo quem emitiu mesmo que o usuário
seja renomeado ou desativado no gateway, e para o front não precisar consultar outro serviço
ao listar notas.

`Processing_id` é a chave de idempotência enviada no `BaixarEstoqueCommand` e devolvida pelo
Estoque em `Inventory_Movements.Idempotency_Key`.

## Products_Tax_Invoice

```sql

CREATE TABLE Products_Tax_Invoice (
    Id                  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    Tax_Invoice_Id      BIGINT       NOT NULL REFERENCES Tax_Invoice (Id) ON DELETE CASCADE,
    Product_id          UUID         NOT NULL,   -- cross-service, sem FK
    Product_code        VARCHAR(50)  NOT NULL,   -- snapshot
    Product_description VARCHAR(200) NOT NULL,   -- snapshot
    Quantity            INTEGER      NOT NULL
);

```

Produto duplicado na mesma nota e quantidade menor ou igual a zero são barrados no serviço,
não no banco.

## Replicated_Products

Read-model alimentado por `OnProdutoCriado` e `OnProdutoAtualizado`. Não guarda saldo — o
saldo é autoridade exclusiva do Estoque.

```sql

CREATE TABLE Replicated_Products (
    Product_id      UUID         NOT NULL PRIMARY KEY,
    Code            VARCHAR(50)  NOT NULL,
    Description     VARCHAR(200) NOT NULL,
    Active          BOOLEAN      NOT NULL,
    Updated_at      TIMESTAMPTZ  NOT NULL
);

```

## Processed_Messages

Deduplicação dos consumidores. A entrega é at-least-once, então todo listener grava aqui
antes de aplicar o efeito.

```sql

CREATE TABLE Processed_Messages (
    Message_id      UUID         NOT NULL PRIMARY KEY,
    Type            VARCHAR(100) NOT NULL,
    Processed_at    TIMESTAMPTZ  NOT NULL
);

```

## Outbox_Messages

Outbox transacional. O evento é gravado na mesma transação da mudança de estado e publicado
depois pelo `OutboxDispatcherWorker`.

```sql

CREATE TABLE Outbox_Messages (
    Id              UUID         NOT NULL PRIMARY KEY,
    Type            VARCHAR(100) NOT NULL,   -- nome do tipo CLR, resolvido pelo dispatcher
    Payload         JSONB        NOT NULL,
    Created_at      TIMESTAMPTZ  NOT NULL,
    Published_at    TIMESTAMPTZ  NULL,
    Attempts        INTEGER      NOT NULL,
    Last_error      TEXT         NULL
);

```
