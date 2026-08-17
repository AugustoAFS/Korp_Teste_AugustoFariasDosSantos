# Testes — NotaFlow

Vitest · TestBed · provideHttpClientTesting

## Localização

O front não tem projetos separados como o backend — o Vitest varre `src/`. A divisão entre unitário e integração é feita por **pasta**, espelhando a mesma ideia dos três serviços.

```
NotaFlow/src/
  testes/
    unitarios/       lógica sem DOM
    integracao/      tela montada de verdade, HTTP mockado
```

## Convenção

A organização segue a **sequência de execução** do front, que é o equivalente do `Controllers/ → Services/ → Repositories/` do backend:

```
Components/  →  Services/  →  Http/
```

O componente é o que o usuário aciona, o serviço carrega a regra, e o HTTP é a borda — guards e interceptors ficam nessa borda porque é onde a requisição passa. O nome do arquivo espelha o do projeto com o sufixo `.spec.ts`, sem a pasta de origem: `core/services/invoices.service.ts` vira `Services/invoices.service.spec.ts`.

## Estado

**Implementado — 91 testes passando** em 14 arquivos.

```bash
cd NotaFlow && npx ng test --watch=false
```

## Estrutura

```
src/testes/unitarios
  Services/
    auth.service.spec.ts               sessão, papéis, permissões derivadas
    invoices.service.spec.ts           o polling: timer, switchMap, parada em printing false
    products.service.spec.ts           criar, atualizar, excluir
    users.service.spec.ts              criar e trocar perfis
    theme.service.spec.ts              os três estados: sistema, claro, escuro
    toast.service.spec.ts              tipos, remoção manual e expiração por tempo
    confirm.service.spec.ts            a promessa que a tela aguarda
    tour.service.spec.ts               navegação dos passos e memória de tour visto
  Http/
    auth.guard.spec.ts                 authGuard e managerGuard
    error.interceptor.spec.ts          cada faixa de status vira a notificação certa
    credentials.interceptor.spec.ts    toda requisição leva o cookie

src/testes/integracao
  Components/
    app.component.spec.ts
    telas.spec.ts                      cada rota monta, busca da API e renderiza
    tour.spec.ts                       os 9 passos, o véu, o botão Próximo
```

### Por que `telas` e `tour` continuam agregados

O espelho por componente não se aplica a esses dois: eles são **transversais por natureza**. O teste de âncoras do tour percorre layout, painel, produtos, notas e usuários numa varredura só, derivada de `TOUR_INICIAL` — quebrá-lo em um arquivo por componente destruiria justamente o que ele verifica, que é a coerência entre os passos e as telas. `telas.spec.ts` segue a mesma lógica: monta cada rota com o mesmo arranjo e compara.

### Ainda sem cobertura

Os componentes de `design-system/` não têm arquivo próprio — `coachmark` é exercitado indiretamente por `tour.spec.ts`, e `toast`/`confirm` têm o serviço coberto mas não a renderização.

## A regra de divisão

**Unitário é o que não monta componente.** Serviços, guards e interceptors. Usa `HttpTestingController` para o HTTP, sem TestBed de componente e sem DOM.

**Integração é a tela montada.** TestBed real, `provideHttpClientTesting`, e a asserção é sobre o que o usuário vê depois que a resposta é servida — não sobre chamadas internas.

> **Precisão de nome:** "integração" aqui é componente + HTTP mockado, não ponta a ponta contra as APIs de verdade. Teste realmente end-to-end (navegador + backend no ar) seria Playwright ou Cypress, e não faz parte desta estrutura.

## Armadilhas já encontradas

Duas coisas quebraram testes neste projeto e valem estar registradas:

- **`whenStable()` trava com `provideHttpClientTesting`.** Ele espera requisições que só resolvem quando o teste as libera com `flush`, e o teste fica pendurado. Use um flush de microtask no lugar.
- **Regressão de âncora do tour.** Os passos dependem de `id` no HTML do layout e do painel; quando esses arquivos são reescritos, os ids somem e o tour vira só um véu escuro. O teste em `telas.spec.ts` é derivado de `TOUR_INICIAL` e falha se alguma âncora sumir — ele não deve ser removido em refatoração de layout.

## O projeto é zoneless

Duas consequências para quem escrever testes aqui:

- **`fakeAsync` e `tick` não existem.** Eles dependem de `zone.js/testing`, que este projeto não carrega. Use `vi.useFakeTimers()` / `vi.advanceTimersByTime()`.
- **Os fake timers precisam ficar dentro do `it`.** Colocá-los num `beforeEach` global derruba o worker do Vitest com `Worker exited unexpectedly`, sem apontar o teste culpado.

## Execução

```bash
cd NotaFlow
npm test                           # modo watch
npx ng test --watch=false          # uma execução, sem watch
```
