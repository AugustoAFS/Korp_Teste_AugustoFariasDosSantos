# Domínio — Serviço de Estoque

**SQL Server** · produtos e autoridade do saldo.

## Produtos

```sql
CREATE TABLE Produtos (
    Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Produtos PRIMARY KEY,
    Codigo               VARCHAR(50)      NOT NULL,
    Descricao            VARCHAR(200)     NOT NULL,
    Saldo                INT              NOT NULL CONSTRAINT CK_Produtos_Saldo CHECK (Saldo >= 0),
    Ativo                BIT              NOT NULL DEFAULT 1,
    CriadoEm             DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CriadoPorUsuarioId   UNIQUEIDENTIFIER NULL,     -- claim do JWT, sem FK
    AlteradoEm           DATETIME2(3)     NULL,
    AlteradoPorUsuarioId UNIQUEIDENTIFIER NULL,     -- claim do JWT, sem FK
    RowVersion           ROWVERSION       NOT NULL
);

```

## MovimentosEstoque

```sql
CREATE TABLE MovimentosEstoque (
    Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MovimentosEstoque PRIMARY KEY,
    ProdutoId      UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_Mov_Produto REFERENCES Produtos (Id),
    Tipo           TINYINT          NOT NULL CHECK (Tipo IN (1,2,3)),   -- 1 Saida, 2 Entrada, 3 Ajuste
    Quantidade     INT              NOT NULL CHECK (Quantidade > 0),
    SaldoAnterior  INT              NOT NULL,
    SaldoNovo      INT              NOT NULL,
    NotaFiscalId   UNIQUEIDENTIFIER NULL,     -- cross-service, sem FK
    IdempotencyKey UNIQUEIDENTIFIER NULL,     -- processamento_id vindo do Faturamento
    UsuarioId      UNIQUEIDENTIFIER NULL,     -- claim do JWT, sem FK
    OcorridoEm     DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME()
);

```

## OutboxMensagens

```sql
CREATE TABLE OutboxMensagens (
    Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OutboxMensagens PRIMARY KEY,
    Tipo         VARCHAR(100)     NOT NULL,
    Payload      NVARCHAR(MAX)    NOT NULL,
    CriadoEm     DATETIME2(3)     NOT NULL DEFAULT SYSUTCDATETIME(),
    PublicadoEm  DATETIME2(3)     NULL,
    Tentativas   INT              NOT NULL DEFAULT 0,
    UltimoErro   NVARCHAR(1000)   NULL
);

CREATE INDEX IX_Outbox_Pendentes ON OutboxMensagens (CriadoEm) WHERE PublicadoEm IS NULL;
```