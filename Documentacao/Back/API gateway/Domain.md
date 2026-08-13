# Domínio — API Gateway

**PostgreSQL** · usuários, perfis e login local (Argon2id).

```sql
CREATE EXTENSION IF NOT EXISTS citext;
```

## usuarios

```sql
CREATE TABLE usuarios (
    id                 UUID         NOT NULL PRIMARY KEY,
    nome               VARCHAR(150) NOT NULL,
    email              CITEXT       NOT NULL,
    email_confirmado   BOOLEAN      NOT NULL DEFAULT FALSE,
    senha_hash         VARCHAR(500) NULL,       -- PHC completo; NULL = só login externo
    pepper_versao      SMALLINT     NOT NULL DEFAULT 1,
    ativo              BOOLEAN      NOT NULL DEFAULT TRUE,
    acessos_falhos     INTEGER      NOT NULL DEFAULT 0,
    bloqueado_ate      TIMESTAMPTZ  NULL,
    senha_alterada_em  TIMESTAMPTZ  NULL,
    criado_em          TIMESTAMPTZ  NOT NULL DEFAULT now()
);

```

## perfis

```sql
CREATE TABLE perfis (
    id         UUID         NOT NULL PRIMARY KEY,
    nome       VARCHAR(50)  NOT NULL,
    descricao  VARCHAR(200) NULL,
    ativo      BOOLEAN      NOT NULL DEFAULT TRUE,
    criado_em  TIMESTAMPTZ  NOT NULL DEFAULT now()
);

```

## usuarios_perfis

```sql
CREATE TABLE usuarios_perfis (
    usuario_id               UUID        NOT NULL REFERENCES usuarios (id) ON DELETE CASCADE,
    perfil_id                UUID        NOT NULL REFERENCES perfis (id),
    atribuido_em             TIMESTAMPTZ NOT NULL DEFAULT now(),
    atribuido_por_usuario_id UUID        NULL,

    PRIMARY KEY (usuario_id, perfil_id)
);

```

## identidades_externas

> **Fora do escopo desta entrega.** Tabela criada, nenhum provedor implementado.
> Chaveada por `(provedor, chave_provedor)` — o `sub` do provedor, não o e-mail,
> que é mutável. Adicionar Google ou Microsoft depois não exige migration.

```sql
CREATE TABLE identidades_externas (
    id              UUID         NOT NULL PRIMARY KEY,
    usuario_id      UUID         NOT NULL REFERENCES usuarios (id) ON DELETE CASCADE,
    provedor        VARCHAR(50)  NOT NULL,   -- 'Google', 'Microsoft'
    chave_provedor  VARCHAR(200) NOT NULL,   -- 'sub' do provedor
    email_provedor  CITEXT       NULL,
    vinculado_em    TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT ux_ident_ext_provedor_chave UNIQUE (provedor, chave_provedor)
);

```