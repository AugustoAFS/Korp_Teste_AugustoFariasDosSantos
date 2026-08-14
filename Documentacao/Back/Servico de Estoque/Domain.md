# Domínio — Serviço de Estoque

**SQL Server** · produtos e autoridade do saldo.

Sem `DEFAULT` e sem `CHECK` no banco: valores iniciais e a recusa de saldo negativo ficam no
código. O débito concorrente é protegido pelo `UPDATE` condicional do repositório, que só
afeta a linha quando o saldo comporta a baixa.

## Products

```sql
CREATE TABLE Products (
    Id                      UNIQUEIDENTIFIER    NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Code                    VARCHAR(50)         NOT NULL,
    Description             VARCHAR(200)        NOT NULL,   -- nome do produto
    Balance                 INT                 NOT NULL,   -- quantidade disponível em estoque
    Active                  BIT                 NOT NULL,
    Created_at              DATETIME2(3)        NOT NULL,
    Updated_at              DATETIME2(3)        NULL,
    Deleted_at              DATETIME2(3)        NULL
);

```

## Inventory_Movements

```sql
CREATE TABLE Inventory_Movements (
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Inventory_Movements PRIMARY KEY,
    Product_id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_Movement_Product REFERENCES Products (Id),
    Type                TINYINT          NOT NULL,   -- 1 Saída, 2 Entrada, 3 Ajuste
    Quantity            INT              NOT NULL,
    Balance_before      INT              NOT NULL,
    Balance_after       INT              NOT NULL,
    Tax_Invoice_id      BIGINT           NULL,       -- cross-service, sem FK
    Idempotency_key     UNIQUEIDENTIFIER NULL,       -- Processing_id vindo do Faturamento
    Moved_by_user_id    BIGINT           NULL,       -- claim sub do JWT, sem FK
    Occurred_at         DATETIME2(3)     NOT NULL
);

```

`Tax_Invoice_id` e `Moved_by_user_id` são `BIGINT` porque a nota fiscal e o usuário nascem de
colunas identity no Faturamento e no gateway. `Idempotency_key` é o que torna o
`OnBaixarEstoque` seguro contra reentrega da mesma mensagem.

## Outbox_Messages

Outbox transacional. O evento é gravado na mesma transação da baixa e publicado depois pelo
`OutboxDispatcherWorker`.

```sql
CREATE TABLE Outbox_Messages (
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Outbox_Messages PRIMARY KEY,
    Type            VARCHAR(100)     NOT NULL,   -- nome do tipo CLR, resolvido pelo dispatcher
    Payload         NVARCHAR(MAX)    NOT NULL,
    Created_at      DATETIME2(3)     NOT NULL,
    Published_at    DATETIME2(3)     NULL,
    Attempts        INT              NOT NULL,
    Last_error      NVARCHAR(MAX)    NULL
);

```
