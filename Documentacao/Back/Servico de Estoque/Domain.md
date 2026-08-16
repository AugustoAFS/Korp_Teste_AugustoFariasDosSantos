# Domínio — Serviço de Estoque

**SQL Server**

## products

```sql
CREATE TABLE products (
    id          UNIQUEIDENTIFIER  NOT NULL CONSTRAINT pk_products PRIMARY KEY,
    code        VARCHAR(50)       NOT NULL,
    description VARCHAR(200)      NOT NULL,
    balance     INT               NOT NULL,
    active      BIT               NOT NULL,
    created_at  DATETIMEOFFSET(3) NOT NULL,
    updated_at  DATETIMEOFFSET(3) NULL,
    deleted_at  DATETIMEOFFSET(3) NULL,

    CONSTRAINT ck_products_balance CHECK (balance >= 0)
);

CREATE UNIQUE INDEX ux_products_code
    ON products (code) WHERE deleted_at IS NULL;
```

## stock_movements

```sql
CREATE TABLE stock_movements (
    id                UNIQUEIDENTIFIER  NOT NULL CONSTRAINT pk_stock_movements PRIMARY KEY NONCLUSTERED,
    product_id        UNIQUEIDENTIFIER  NOT NULL REFERENCES products (id),
    type              TINYINT           NOT NULL,   -- 1 Outbound, 2 Inbound, 3 Adjustment
    quantity          INT               NOT NULL,
    balance_before    INT               NOT NULL,
    balance_after     INT               NOT NULL,
    invoice_id        BIGINT            NULL,       -- cross-service, sem FK
    idempotency_key   UNIQUEIDENTIFIER  NULL,       -- ProcessamentoId vindo do Faturamento
    moved_by_user_id  BIGINT            NULL,       -- claim sub do JWT, sem FK
    occurred_at       DATETIMEOFFSET(3) NOT NULL
);

CREATE CLUSTERED INDEX ix_stock_movements_occurred_at ON stock_movements (occurred_at);

CREATE UNIQUE INDEX ux_stock_movements_idempotency
    ON stock_movements (idempotency_key, product_id) WHERE idempotency_key IS NOT NULL;

CREATE INDEX ix_stock_movements_product ON stock_movements (product_id, occurred_at);
```
## processed_messages

```sql
CREATE TABLE processed_messages (
    message_id      UNIQUEIDENTIFIER  NOT NULL CONSTRAINT pk_processed_messages PRIMARY KEY,
    type            VARCHAR(100)      NOT NULL,
    processed_at    DATETIMEOFFSET(3) NOT NULL,
    outcome_type    VARCHAR(100)      NULL,   -- evento resultante, gravado na mesma transação da baixa
    outcome_payload NVARCHAR(MAX)     NULL    -- reemitido se o mesmo processamento chegar de novo
);
```

## outbox_messages

```sql
CREATE TABLE outbox_messages (
    id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT pk_outbox_messages PRIMARY KEY NONCLUSTERED,
    type         VARCHAR(100)      NOT NULL,   -- nome do tipo CLR, resolvido pelo dispatcher
    payload      NVARCHAR(MAX)     NOT NULL,
    created_at   DATETIMEOFFSET(3) NOT NULL,
    published_at DATETIMEOFFSET(3) NULL,
    attempts     INT               NOT NULL,
    last_error   NVARCHAR(MAX)     NULL
);

CREATE CLUSTERED INDEX ix_outbox_messages_created_at ON outbox_messages (created_at);

CREATE INDEX ix_outbox_messages_pending
    ON outbox_messages (published_at, attempts, created_at) WHERE published_at IS NULL;
```