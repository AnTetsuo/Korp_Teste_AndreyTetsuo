# Korp — Estoque e Faturamento

Sistema de emissão e impressão de notas fiscais composto por **dois microsserviços .NET 10** e um
**frontend Angular 21**:

- **Korp.Stock** — produtos e saldos, com razão (ledger) de movimentações.
- **Korp.Invoicing** — notas fiscais, itens e o fluxo de impressão.
- **frontend** — telas de produtos e notas, incluindo a impressão com acompanhamento em tempo real.

A comunicação entre os serviços é **assíncrona por mensagens** (RabbitMQ via Wolverine, com
inbox/outbox durável em PostgreSQL). `POST /invoices/{id}/print` move a nota para *Processando* e
pede ao estoque que baixe as linhas; o estoque aplica ou recusa e responde; a invoicing fecha a nota
ou a reabre com o motivo — sem nenhum clique na segunda metade.

**308 testes automatizados passam**: 87 no stock, 122 na invoicing e 99 no frontend.

---

## Tecnologias utilizadas

| Camada | Tecnologia |
|---|---|
| Frontend | Angular 21.2 (**zoneless** + signals), Angular Material + CDK 21.2, RxJS 7.8, TypeScript 5.9 |
| Backend | .NET 10 / ASP.NET Core **Minimal APIs**, Clean Architecture |
| ORM | EF Core 10 + Npgsql 10 + EFCore.NamingConventions (snake_case) |
| Banco de dados | PostgreSQL 18 — **um banco por serviço** (`stock` e `invoicing`) |
| Mensageria | RabbitMQ 4 via **Wolverine 6.29** — inbox/outbox durável, retries, dead letters, mensagens agendadas |
| Validação | FluentValidation 12 (formato) + fábricas de domínio (regra de negócio) |
| Erros | RFC 9457 Problem Details, com `traceId` em todo payload |
| Logs | Serilog 10 |
| Documentação da API | OpenAPI + Scalar 2.16 (em `/scalar/v1`, sob flag) |
| Configuração | DotNetEnv 3.2 — um único `.env` na raiz |
| Testes | xUnit 2.9 + Shouldly 4.3 (backend), Vitest 4 (frontend) |
| Empacotamento | Docker + Docker Compose |

**Nota de licenciamento:** MediatR 13+, FluentAssertions 8+ e MassTransit 9 passaram a exigir licença
comercial. Por isso `Result`, `ValidationError` e `ICommandHandler<,>` são escritos à mão neste
repositório, e os testes usam Shouldly.

---

## O que é necessário para abrir o projeto

| Requisito | Versão | Observação |
|---|---|---|
| .NET SDK | **10.0** | `dotnet --version` |
| Docker Desktop | recente | sobe PostgreSQL, RabbitMQ e as duas APIs |
| Node.js | **≥ 20.19** (recomendado **24.18.0**) | `frontend/.nvmrc` fixa 24.18.0 — o CLI do Angular 21 recusa versões antigas |
| dotnet-ef | 10.x | `dotnet tool install --global dotnet-ef` — necessário para aplicar as migrations |

### Portas e endereços

| Serviço | URL | Porta (`.env`) |
|---|---|---|
| Frontend (`ng serve`) | http://localhost:4200 | — |
| Korp.Stock API | http://localhost:3000 | `STOCK_API_PORT` |
| Korp.Invoicing API | http://localhost:3001 | `INVOICING_API_PORT` |
| PostgreSQL | localhost:5432 | `POSTGRES_PORT` |
| RabbitMQ (AMQP) | localhost:5672 | `RABBITMQ_PORT` |
| RabbitMQ (management UI) | http://localhost:15672 | `RABBITMQ_MANAGEMENT_PORT` |

Cada API expõe ainda `/health/live`, `/health/ready` e — com `API_DOCS_ENABLED=true` — `/scalar/v1`.

---

## Configuração

**Todo o projeto lê um único `.env` na raiz do repositório.** Ele é gitignorado; `.env.example`
documenta as chaves. Copie e preencha as senhas:

```bash
cp .env.example .env
```

```dotenv
POSTGRES_PASSWORD=escolha-uma-senha
ConnectionStrings__Stock=Host=localhost;Port=5432;Database=stock;Username=korp;Password=escolha-uma-senha;Search Path=stock
ConnectionStrings__Invoicing=Host=localhost;Port=5432;Database=invoicing;Username=korp;Password=escolha-uma-senha;Search Path=invoicing
RABBITMQ_PASSWORD=escolha-uma-senha
RabbitMq__Password=escolha-uma-senha
```

Três detalhes que evitam confusão:

- As chaves `ConnectionStrings__*` e `RabbitMq__*` apontam para `localhost` porque servem ao **host**
  (para `dotnet ef`, `dotnet run` e o comando `resources setup`). O Compose **sobrescreve** essas
  variáveis dentro da rede, onde os hosts são `korp-db` e `rabbitmq`.
- `Cors:AllowedOrigins` já vem com `http://localhost:4200`, que é onde o `ng serve` roda. O frontend
  chama **as duas APIs diretamente**
- O frontend lê as URLs das APIs de `frontend/public/config.json` **em tempo de execução**, não de um
  `environment.ts` compilado. O arquivo já vem com os valores de desenvolvimento.

---

## Como rodar o programa

### 1. Suba a infraestrutura

```bash
docker compose up -d korp-db rabbitmq
```

Na primeira execução, `docker/postgres-init/` cria os bancos `stock` e `invoicing`.

### 2. Aplique as migrations

**As migrations não rodam no startup** — o schema é um passo de deploy e
não um efeito colateral de subir um container.

```bash
dotnet ef database update --project stock/src/Korp.Stock.Infrastructure
```

```bash
dotnet ef database update --project invoicing/src/Korp.Invoicing.Infrastructure
```

> `dotnet ef` deve apontar **só** para o projeto de Infrastructure: nenhum projeto de Api referencia
> `Microsoft.EntityFrameworkCore.Design`, então passar `--startup-project` falha. As factories de
> `DbContext` carregam o `.env` sozinhas, subindo os diretórios até a raiz.

### 3. Crie as tabelas de mensageria e as filas

As tabelas de envelope do Wolverine também são schema, e também não são criadas no startup
(`AutoBuildMessageStorageOnStartup = AutoCreate.None`):

```bash
dotnet run --project stock/src/Korp.Stock.Api -- resources setup
```

```bash
dotnet run --project invoicing/src/Korp.Invoicing.Api -- resources setup
```

### 4. Suba as duas APIs

```bash
docker compose up -d --build
```

<details>
<summary>Alternativa: rodar as APIs no host, sem container</summary>

```bash
dotnet run --project stock/src/Korp.Stock.Api
```

```bash
dotnet run --project invoicing/src/Korp.Invoicing.Api
```
</details>

### 5. Suba o frontend

```bash
nvm use 24.18.0
```

```bash
cd frontend && npm ci && npm start
```

Abra **http://localhost:4200**.

---

## Endpoints

### Korp.Stock — http://localhost:3000

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/products` | Cria um produto e abre o saldo inicial |
| `GET` | `/products` | Lista produtos — paginado, filtrado e ordenado |

### Korp.Invoicing — http://localhost:3001

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/invoices` | Abre uma nota com suas linhas |
| `GET` | `/invoices` | Lista notas — paginado, filtrado e ordenado |
| `GET` | `/invoices/{id}` | Lê uma nota com itens e o resultado da impressão — **é o que o cliente faz polling** |
| `POST` | `/invoices/{id}/print` | **202 Accepted**; move a nota para *Processando* |

`POST /print` responde **202, não 200**: os saldos ainda não se moveram. A mudança de status e a
mensagem de saída são gravadas **na mesma transação**, então o pedido é durável no instante em que a
resposta volta — mas o estoque aplica de forma assíncrona, e a nota só chega a *Fechada* quando o
estoque responde. **409** se a nota não estiver *Aberta*, inclusive num duplo clique em *Imprimir*.

Não há rota HTTP que movimente estoque: depois que `POST /products` abre o saldo, **toda**
movimentação posterior chega por mensagem.

---

## Arquitetura

```
                        ┌──────────────────────────┐
                        │    Angular 21   :4200    │
                        │    produtos · notas      │
                        └───┬──────────────────┬───┘
                            │                  │
        ┌───────────────────┘                  └───────────────────┐
        │ HTTP                                                HTTP │
        │ GET /products                             POST /invoices │
        │                                POST /invoices/{id}/print │
        v                                                          v
┌────────────────────┐     ┌────────────────────┐     ┌────────────────────┐
│ Korp.Stock  :3000  │<--->│   RabbitMQ 4       │<--->│ Korp.Invoicing     │
│ produtos, saldos   │     │   (Wolverine)      │     │ :3001   notas      │
└──────────┬─────────┘     └────────────────────┘     └──────────┬─────────┘
           │                                                     │
           v                                                     v
┌────────────────────┐                                ┌────────────────────┐
│ PostgreSQL 18      │                                │ PostgreSQL 18      │
│ banco `stock`      │                                │ banco `invoicing`  │
│ dados + envelopes  │                                │ dados + envelopes  │
│ stock_messaging    │                                │ invoicing_messaging│
└────────────────────┘                                └────────────────────┘
```

**Nenhum serviço chama o outro por HTTP.** Toda comunicação entre eles passa pelo broker, e cada
mensagem tem fila e sentido fixos:

| Mensagem | Publica | Fila | Consome |
|---|---|---|---|
| `invoice-print-requested` | Invoicing | `stock-operation` | Stock |
| `stock-operation-applied` | Stock | `invoicing-operation-replies` | Invoicing |
| `stock-operation-rejected` | Stock | `invoicing-operation-replies` | Invoicing |

> **Nota — uma quarta mensagem.** `PrintTimeoutCheck` não aparece na tabela porque não é
> comunicação entre serviços: a invoicing a agenda **para si mesma**, na mesma transação em que a
> nota entra em *Processando*. Ela reenvia o pedido enquanto restam tentativas (30 s / 60 s / 120 s)
> e reabre a nota quando elas acabam — é o que resgata uma impressão cuja resposta nunca voltou.

As respostas vão para uma **fila declarada**, nunca por `RespondToSender`: o header `reply-uri` aponta
para uma fila efêmera por nó, e se o outro serviço reiniciar antes de a resposta chegar, a fila
sumiu — e a resposta junto, de forma indistinguível do silêncio que o timeout deveria pegar.

Uma mensagem também **não vai direto ao RabbitMQ**: ela é gravada primeiro na tabela de outbox, no
banco do próprio serviço, **na mesma transação** que a mudança de estado, e só então é despachada.
É por isso que os envelopes aparecem no diagrama dentro de cada banco, e não junto do broker.

Os dois serviços são **não compartilham biblioteca**: os contratos de mensagem são
duplicados, algo a se propor é avançar para um mono-repo com uma class library `messaging` compartilhada
entre os dois 

Cada serviço segue Clean Architecture — `Domain` ← `Application` ← `Infrastructure` / `Api`. Nada
referencia `Api`, e `Application` nunca referencia `Infrastructure`.

---

## Fluxos principais

### Criação de nota

1. A tela de nova nota busca produtos no **stock** (`GET /products`, com `debounceTime` +
   `switchMap`) e posta a nota na **invoicing** (`POST /invoices`).
2. `Invoice.Open` valida no domínio: número obrigatório, ao menos um item, quantidades positivas e
   **nenhum produto repetido**. Os itens guardam um *snapshot* de código e descrição — uma nota é um
   documento e não pode se reescrever quando um produto é renomeado.
3. A nota nasce **Aberta**. Nada de estoque se move ainda.

### Impressão

1. `POST /invoices/{id}/print` → `Invoice.BeginPrinting()` — que **é** a regra "não permitir impressão
   de nota com status ≠ Aberta". O status vai a *Processando* e a mensagem `invoice-print-requested`
   é gravada na mesma transação. Um *timeout* também é agendado aqui, então nenhuma nota chega a
   *Processando* sem lembrete.
2. O **stock** consome, aplica as baixas e responde `stock-operation-applied`; se faltar saldo,
   responde `stock-operation-rejected` com **dados estruturados** — código do erro e, por linha,
   quanto foi solicitado e quanto havia.
3. A **invoicing** consome a resposta: fecha a nota, ou a reabre registrando o motivo. Uma nota
   reaberta guarda **por que** — `insufficient_stock` quando o estoque recusou, `print_timeout`
   quando ninguém respondeu a tempo. São situações diferentes e a tela precisa dizer coisas
   diferentes.
4. A tela faz polling de `GET /invoices/{id}` a cada segundo e para sozinha quando o status sai de
   *Processando*. A recusa é traduzida para português usando o snapshot de código da própria nota:
   *"ATL0138: 6 em estoque, 506 solicitadas"* em vez do GUID cru; o timeout diz *"O estoque não
   confirmou esta impressão a tempo"*, e só nesse caso a tela segura o motivo por 5 s — uma
   confirmação atrasada ainda pode fechar a nota, e mandar "tente de novo" no instante em que o
   sistema pode terminar sozinho é o pior conselho possível.

---

## Tratamento de falhas

| Situação | Resposta |
|---|---|
| Corpo inválido, campo faltando, tipo errado | **400** Problem Details com `errors` por campo |
| Nota inexistente | **404** |
| Nota não está *Aberta* (inclui duplo clique) | **409** |
| Duas impressões simultâneas | uma **202**, uma **409**, **uma** mensagem publicada |
| Saldo insuficiente (caminho de mensagem) | o handler **responde** `stock-operation-rejected`; a nota reabre com `insufficient_stock` e as linhas culpadas |
| Estoque não responde | o timeout agendado reenvia o pedido e, esgotadas as tentativas, reabre a nota com `print_timeout` |
| Falha de infraestrutura no consumo | a exceção **escapa de propósito**; a política reexecuta com cooldowns com jitter e retentativas duráveis |

**Falha esperada não é exceção.** Todo caso de uso retorna `Result`/`Result<T>`; exceção fica para o
que é genuinamente excepcional — o que também define o que a mensageria deve tentar de novo. Saldo
insuficiente jamais lança: lançar faria o Wolverine retentar eternamente algo que nunca vai passar.

**Concorrência pelo `xmin` do Postgres** (sem coluna de versão): quem perde a corrida afeta zero
linhas e recebe 409, sem read-then-write.

**Recuperação**, o cenário pedido pelo enunciado — com o estoque **parado**, a nota fica em
*Processando*; ao subir o estoque, a fila drena e a nota se resolve sozinha, com a tela aberta e sem
nenhum clique. Três peças sustentam isso:

- **Outbox transacional** — estado e envelope na mesma transação (comprovado comparando o `xmin` das
  três linhas).
- **Timeout como mensagem agendada**, não varredura — 30 s / 60 s / 120 s e então a nota reabre com
  `print_timeout`, que é o que permite à tela distinguir "ninguém respondeu" de "o estoque recusou".
- **Idempotência em duas camadas** — o inbox durável descarta uma *reentrega*; o índice único de
  `entity_references` descarta uma *intenção repetida*, que o inbox não teria como reconhecer.

---

## Testes

```bash
dotnet test stock/Korp.Stock.sln
```

```bash
dotnet test invoicing/Korp.Invoicing.sln
```

```bash
cd frontend && npm test
```

87 + 122 + 99 = **308 testes**, cobrindo o domínio, os validators, o parsing de Problem Details, os
componentes e o fluxo de polling.

---

## Estrutura do repositório

```
docker-compose.yml          orquestra Postgres, RabbitMQ e as duas APIs
.env / .env.example         fonte única de configuração
docker/postgres-init/       cria um banco por serviço no primeiro boot
stock/                      o serviço de estoque
  Dockerfile                contexto de build ./stock
  src/                      Korp.Stock.Api · .Application · .Domain · .Infrastructure
  tests/                    Korp.Stock.UnitTests
invoicing/                  o serviço de faturamento — espelha o stock arquivo a arquivo
  src/                      Korp.Invoicing.Api · .Application · .Domain · .Infrastructure
  tests/                    Korp.Invoicing.UnitTests
frontend/                   o frontend Angular
  public/config.json        URLs das APIs lidas em tempo de execução
  src/app/core/             config, api/stock, api/invoicing, http
  src/app/features/         products/ · invoicing/{list,create,detail}/
  src/app/shared/           invoice-status.pipe.ts, paginator-intl.ts, print-failure.ts
```

---

## Requisitos opcionais

| Item | Status |
|---|---|
| **a.** Concorrência — saldo 1, duas notas | ✅ Implementado. `xmin` + retentativas com jitter: uma nota fecha, a outra é recusada com o motivo |
| **b.** Inteligência artificial | ❌ Fora de escopo por tempo |
| **c.** Idempotência | ✅ Implementado em duas camadas — inbox durável e índice único em `entity_references` |
