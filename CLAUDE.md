# CLAUDE.md

Guidance for working in this repository.

**Language.** The API-Gateway is written in **English** — types, members, folders, DB tables and columns, JSON payloads, error codes. Only human-readable text stays Portuguese: `ProblemDetails` titles/details (they reach the end user) and log messages. The older code in API-Estoque is still pt-BR throughout; when you touch a service, follow the language that service already uses, and prefer English for anything new.

## What this is

`emissor-nf` — an electronic invoice (NF-e / "Nota Fiscal") emitter, built as event-driven microservices. It's a technical assessment project. A user creates products, opens an invoice, adds items, and "prints" it; printing debits stock from the inventory service. If any item lacks balance, the whole invoice is rejected (all-or-nothing).

**Current state: partially built.** The `Documentacao/` folder describes the *intended* full design in detail. Implemented today: the **API-Gateway** (open registration + login, cookie session, antiforgery, internal JWT, rate limit, EF migration, roles seed, and **YARP** proxying `/api/v1/produtos` to Estoque — the transform swaps the session cookie for a signed internal JWT, so the front only ever talks to the gateway with a cookie; the Polly circuit breaker is still **not** implemented) and the **API-Estoque in full** (produtos CRUD, saldo authority, transactional outbox, `OnBaixarEstoque` saga participation, EF migration). Faturamento is still template code (default `Program.cs`, empty `AppDbContext`, `Class1.cs` stubs, `.gitkeep`-only folders); the Angular app is the CLI skeleton. Treat `Documentacao/` as the design spec (source of truth for intent), and the code as the current, partial reality — don't assume a documented class exists until you've checked.

**No comments in code.** New code carries no `//`, `///` or `/* */` — naming and structure carry the intent. The older messaging code in API-Estoque still has comments; don't take it as the standard.

**One type per file, and no fallbacks.** Every file declares exactly one type (interface and implementation live in separate files). Configuration is required and validated at startup — no `?? new Opcoes...()`, no default property values, no seeded "emergency" user. A missing section or invalid value must crash the boot naming the offending key.

## Components

| Path | Role | Stack / DB |
|---|---|---|
| `API-Gateway/` (`Gateway`) | Single entry point: auth (Argon2id + HttpOnly cookie), YARP reverse proxy, signs internal JWT for downstream (Polly circuit breaker not implemented) | .NET 10, MVC (no Clean Arch — no domain of its own), PostgreSQL |
| `API-Faturamento/` | Invoices (`notas_fiscais`), items, print lifecycle. Does **not** store stock balance. Publishes `BaixarEstoqueCommand`; consumes stock results | .NET 10, Clean Architecture, PostgreSQL |
| `API-Estoque/` | Authority over product stock balance. Consumes `BaixarEstoqueCommand`; publishes `EstoqueBaixado` / `EstoqueRejeitado` and product events | .NET 10, Clean Architecture, SQL Server |
| `NotaFlow/` | Frontend | Angular 21, standalone components, signals, Angular Material, Vitest |

Each backend service has its own `.slnx` solution; there is no top-level solution tying them together.

## Architecture patterns (the parts that matter)

- **Clean Architecture** (Estoque, Faturamento). Dependency direction, enforced by project references:
  `Api → ApplicationService · InfraStructure · EventListeners`; `ApplicationService → Domain`; `InfraStructure → Domain`; `EventListeners → ApplicationService · InfraStructure`. Domain depends on nothing. Keep new code on the correct side — e.g. domain contracts (`IOutboxRepository`) live in `*.Domain/Interfaces`, message contracts and MassTransit wiring live only in `*.EventListeners`.

- **Transactional outbox.** State-changing operations write the event to an outbox table *in the same DB transaction*. `OutboxDispatcherWorker` (a `BackgroundService`, polls every 2s, batches of 50) publishes to RabbitMQ afterward. Delivery is **at-least-once**, so every consumer must be **idempotent** (Faturamento has a `mensagens_processadas` table for dedup). The outbox stores `Tipo` (CLR type name) + JSON `Payload`; the worker keeps a name→`Type` map to deserialize and publish typed.

- **MassTransit conventions** (`EventListenerService.AddEventListeners`). `AddConsumers(assembly)` scans for consumers; `ConfigureEndpoints` creates queues from the nested `ConsumerDefinition` in each `On*` listener — **adding a listener needs no change to the registration code**. Each message contract carries `[MessageUrn("emissor:...")]` — note the **missing** `urn:message:` prefix, see the gotcha below; the resulting URN **must be identical on both the publishing and consuming service**, or the message sits in the queue with no consumer.

- **Invoice print = choreographed saga.** `Faturamento` publishes `BaixarEstoqueCommand` → `Estoque.OnBaixarEstoque` debits (all-or-nothing) and publishes `EstoqueBaixado` or `EstoqueRejeitado` → `Faturamento` consumes to close or reject the invoice. Correctness of concurrent debits is enforced at the DB (conditional `UPDATE` + `CHECK (Saldo >= 0)`), which is why `OnBaixarEstoque` deliberately runs `ConcurrentMessageLimit = 5` rather than serializing.

- **Cross-service references carry no FK** (e.g. `NotaFiscalId`, `ProdutoId`, `UsuarioId` from a JWT claim). Snapshots (product code/description, emitter name) are copied into the invoice so it stays stable if the source changes.

## Commands

Run from each directory. `.slnx` requires a recent .NET SDK / Visual Studio 2022 17.13+ (SDK 10.0.400 is installed here).

**Infrastructure** (from repo root) — starts only the databases + RabbitMQ, *not* the app services:
```bash
docker compose up -d          # gateway-db :5432, faturamento-db :5433 (Postgres),
                              # estoque-db :1433 (SQL Server), rabbitmq :5672 / mgmt :15672
```
RabbitMQ management UI: http://localhost:15672 (Admin / Admin, vhost `emissor`). All ports bind to `127.0.0.1` only.

**Backend** (per service dir, e.g. `API-Estoque/`):
```bash
dotnet build                          # build the .slnx
dotnet run --project Estoque.Api      # run one service (Estoque.Api → http://localhost:5247)
```

**Frontend** (`NotaFlow/`):
```bash
npm install
npm start        # ng serve → http://localhost:4200
npm run build    # ng build → dist/
npm test         # ng test (Vitest)
```

There is no automated test project for the backend yet.

## Conventions & gotchas

- **Portuguese names throughout** — `Estoque` (stock/inventory), `Faturamento` (billing/invoicing), `Baixa`/`Baixar` (debit stock), `Nota Fiscal` (invoice), `Saldo` (balance), `Produto`, `Usuario`. New identifiers should follow suit.
- **Message URNs are a contract.** When adding or changing a cross-service message, update the `[MessageUrn]` on both sides together, and add its CLR type to the dispatcher's type map in `OutboxDispatcherWorker`. The attribute value **must not** include the `urn:message:` prefix — MassTransit prepends it and throws `ArgumentException` if it is already there. Write `[MessageUrn("emissor:baixar-estoque")]` to get `urn:message:emissor:baixar-estoque` on the wire. The throw happens inside the attribute constructor, so it only surfaces when something reflects over the type (e.g. `System.Text.Json` serializing it into the outbox) — not at compile time, and not at startup.

- **YARP route paths need two routes per resource.** A catch-all cannot share a segment with a literal, so `"/api/v1/produtos{**resto}"` fails at startup with `RoutePatternException` and takes the whole gateway down. Declare the collection and the item separately: `"/api/v1/produtos"` **and** `"/api/v1/produtos/{**resto}"`, both pointing at the same `ClusterId`. Give both `"AuthorizationPolicy": "default"` — a YARP route is anonymous unless it says otherwise, and the app-wide `FallbackPolicy` does **not** cover it.
- **No user secrets — everything lives in the committed `appsettings.json`**, connection strings and `Security:JwtKey` / `Security:Pepper` included, so the project runs on `git clone` + `dotnet run`. These are development keys, versioned on purpose for the same reason as the seeded admin. Do not reintroduce `dotnet user-secrets`. `Security:JwtKey` must stay **identical** in Gateway and Estoque; changing `Security:Pepper` invalidates every stored password. Never add a `${VAR:-}` env var for these in `docker-compose.yml` — an empty default still *sets* the variable, overrides `appsettings.json`, and crashes the boot on the required-setting check.
- The frontend design system (colors, typography, per-section theming, component specs, accessibility checklist) is fully specified in `Documentacao/Front/DesignSystem.md` and `Telas.md` — consult those before building UI rather than inventing styles.
- `.slnx` is the newer XML solution format; there are no `.sln` files.
