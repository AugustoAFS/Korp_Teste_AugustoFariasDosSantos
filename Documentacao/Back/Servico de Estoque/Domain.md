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

```

## stock_movements

```sql
CREATE TABLE stock_movements (
    id                UNIQUEIDENTIFIER  NOT NULL CONSTRAINT pk_stock_movements PRIMARY KEY NONCLUSTERED,
    product_id        UNIQUEIDENTIFIER  NOT NULL REFERENCES products (id),
    type              TINYINT           NOT NULL,
    quantity          INT               NOT NULL,
    balance_before    INT               NOT NULL,
    balance_after     INT               NOT NULL,
    invoice_id        BIGINT            NULL,
    idempotency_key   UNIQUEIDENTIFIER  NULL,
    moved_by_user_id  BIGINT            NULL,
    occurred_at       DATETIMEOFFSET(3) NOT NULL
);

```
## processed_messages

```sql
CREATE TABLE processed_messages (
    message_id      UNIQUEIDENTIFIER  NOT NULL CONSTRAINT pk_processed_messages PRIMARY KEY,
    type            VARCHAR(100)      NOT NULL,
    processed_at    DATETIMEOFFSET(3) NOT NULL,
    outcome_type    VARCHAR(100)      NULL,
    outcome_payload NVARCHAR(MAX)     NULL
);
```

## outbox_messages

```sql
CREATE TABLE outbox_messages (
    id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT pk_outbox_messages PRIMARY KEY NONCLUSTERED,
    type         VARCHAR(100)      NOT NULL,  
    payload      NVARCHAR(MAX)     NOT NULL,
    created_at   DATETIMEOFFSET(3) NOT NULL,
    published_at DATETIMEOFFSET(3) NULL,
    attempts     INT               NOT NULL,
    last_error   NVARCHAR(MAX)     NULL
);

```