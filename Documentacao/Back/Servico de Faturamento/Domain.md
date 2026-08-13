# Domínio — Serviço de Faturamento

**PostgreSQL** · notas fiscais, itens e ciclo de emissão. Não guarda saldo.

```sql
CREATE SEQUENCE seq_nota_fiscal_numero AS BIGINT START WITH 1 INCREMENT BY 1;
```

## notas_fiscais

```sql
CREATE TABLE notas_fiscais (
    id                        UUID         NOT NULL PRIMARY KEY,
    numero                    BIGINT       NOT NULL DEFAULT nextval('seq_nota_fiscal_numero'),
    status                    SMALLINT     NOT NULL DEFAULT 1 CHECK (status IN (1, 2)),  -- 1 Aberta, 2 Fechada
    emitida_por_usuario_id    UUID         NOT NULL,   -- claim do JWT, sem FK
    emitida_por_usuario_nome  TEXT         NOT NULL,   -- snapshot
    criada_em                 TIMESTAMPTZ  NOT NULL DEFAULT now(),
    fechada_em                TIMESTAMPTZ  NULL,
    processamento_id          UUID         NULL,       -- impressão em voo / idempotência
    processamento_iniciado_em TIMESTAMPTZ  NULL,
    ultimo_erro               TEXT         NULL
);

```

## itens_nota_fiscal

```sql
CREATE TABLE itens_nota_fiscal (
    id                 UUID         NOT NULL PRIMARY KEY,
    nota_fiscal_id     UUID         NOT NULL REFERENCES notas_fiscais (id) ON DELETE CASCADE,
    produto_id         UUID         NOT NULL,   -- cross-service, sem FK
    produto_codigo     VARCHAR(50)  NOT NULL,   -- snapshot
    produto_descricao  VARCHAR(200) NOT NULL,   -- snapshot
    quantidade         INTEGER      NOT NULL CHECK (quantidade > 0),

    CONSTRAINT ux_item_nf_produto UNIQUE (nota_fiscal_id, produto_id)
);

```

## produtos_replicados  (read-model, sem saldo)

```sql
CREATE TABLE produtos_replicados (
    produto_id     UUID         NOT NULL PRIMARY KEY,
    codigo         VARCHAR(50)  NOT NULL,
    descricao      VARCHAR(200) NOT NULL,
    ativo          BOOLEAN      NOT NULL DEFAULT TRUE,
    atualizado_em  TIMESTAMPTZ  NOT NULL DEFAULT now()
);

```

## mensagens_processadas

```sql
CREATE TABLE mensagens_processadas (
    message_id     UUID         NOT NULL PRIMARY KEY,
    tipo           VARCHAR(100) NOT NULL,
    processado_em  TIMESTAMPTZ  NOT NULL DEFAULT now()
);
```

## outbox_mensagens

```sql
CREATE TABLE outbox_mensagens (
    id            UUID         NOT NULL PRIMARY KEY,
    tipo          VARCHAR(100) NOT NULL,
    payload       JSONB        NOT NULL,
    criado_em     TIMESTAMPTZ  NOT NULL DEFAULT now(),
    publicado_em  TIMESTAMPTZ  NULL,
    tentativas    INTEGER      NOT NULL DEFAULT 0,
    ultimo_erro   TEXT         NULL
);

```