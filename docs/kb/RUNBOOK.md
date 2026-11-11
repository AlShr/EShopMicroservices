---
title: Project knowledge runbook (gstack + gbrain)
tags: [eshop, kb, runbook]
updated: 2026-11-11
---

# Project knowledge runbook (gstack + gbrain)

A step-by-step guide to index this project, build a knowledge base in gbrain,
test cross-session memory, and adopt gstack skills. Phases 1-5. Reusable on
other projects (see the last section).

## How it fits together

Knowledge about the project lives in three places:

| Layer | What's in it | How it gets there | How you read it |
|---|---|---|---|
| **Code index** (source `gstack-code-6651f992-3f84fc`) | Every file, symbol and call | `/sync-gbrain` | `gbrain code-def`, `code-callers`, `search` |
| **Knowledge base pages** (source `default`) | Architecture, decisions, flows, tech debt: anything you can't read off a single file | You write a `.md` file, then `gbrain put` | `gbrain search ... --source-id default`, `gbrain get <slug>` |
| **Facts** | Short statements like "we chose X because Y" | `gbrain remember` or a prompt to Claude | `gbrain recall` |

> ⚠️ **Inside this repo, `gbrain search` only searches the code index** (the
> repo is pinned to it through `.gbrain-source`). To find knowledge base
> pages, add `--source-id default`, or `--source-id __all__` to search everything.

**Run terminal commands from the repo root.** In Claude Code, put `!` in front
of a command (`! gbrain search "..."`) so Claude sees its output too.

**Where to save things.** Keep each knowledge base page as a Markdown file in
`docs/kb/` and commit it. Git is the reviewable source of truth, and gbrain is
the search index over it.

```
docs/kb/
  RUNBOOK.md            <- this file
  architecture.md
  interactions.md
  tech-stack.md
  adrs.md
  business-logic.md
  tech-debt.md
  _verification/
    phase1-scorecard.md
    phase3-memory-test.md
    phase4-skills.md
```

**Before you start:** Docker Desktop must be running (gbrain's Postgres
`gbrain-postgres` runs there). Quick check: `docker ps` and `gbrain doctor`.

---

## Phase 1: Index and check retrieval (~30 min)

**1.1 Sync.** In Claude Code:

```
/sync-gbrain --full
/sync-gbrain --dream
```

Pass when both end with `OK code` and `OK dream`. If the memory row shows
`ERR`, run `/sync-gbrain` once more. That error is transient.

**1.2 Find definitions.** Pick about 15 class names from different services
and look each one up:

```
! gbrain code-def CachedBasketRepository
! gbrain code-def DiscountService
! gbrain code-def ApplicationDbContext
```

Pass when `count` ≥ 1 and the file path is correct.
Known limitation: `record` types (commands, queries, events) always return 0.
Use `gbrain search` or Grep for them.

**1.3 Call graph.** Always query a **method** name, never a class name:

```
! gbrain code-callees DeductDiscount        # expect GetDiscountAsync (Basket→Discount gRPC)
! gbrain code-callers Publish               # expect CheckoutBasketCommandHandler.Handle
! gbrain code-callers CreateNewOrder        # expect CreateOrderHandler.Handle
! gbrain code-callers GetBasket             # who reads baskets
```

Read the `from_symbol_qualified` field to see who the caller is. Edges are not
linked to the method they call (`resolved=false`), so methods with the same
name (`Handle`, `Send`) get merged. Calls between services (gRPC,
RabbitMQ) do not appear in the graph at all; they belong in `interactions.md`.

**1.4 Plain-English questions.** Ask about 10 questions where you already know
the answer:

```
! gbrain search "where is the discount applied to the basket price" --limit 5
! gbrain query "how does checkout create an order"
```

Pass when the correct file is in the top 5.

**1.5 Save the results** in `docs/kb/_verification/phase1-scorecard.md`: each
check with hit/miss and a score.

Baseline (2026-11-11, gbrain 0.60.645.0, commit 9b924da):

| Check | Score |
|---|---|
| Finding definitions of classes and interfaces | 17/17 |
| Finding definitions of C# records | 0 (not indexed; 87 records in repo) |
| Who-calls-what between methods | 8/8 |
| Each call linked to the method it calls | none are linked |
| Plain-English questions, top 5 | 11/12 |

---

## Phase 2: Build the knowledge base (~1 page per sitting)

Repeat this loop for each of the 6 pages.

**Step A. Ask Claude to write the draft.** Copy this prompt and change the
topic line:

> Write `docs/kb/interactions.md`. Topic: every interaction between services:
> sync (HTTP, gRPC, YARP routes) and async (MassTransit events, who publishes,
> who consumes). Use gbrain first (`code-def`, `code-callers`, `search`), then
> read the files to confirm. Back every claim with a `path:line` reference.
> Mark anything you could not verify as **UNVERIFIED**. Start the file with
> frontmatter: `title`, `tags: [eshop, kb]`, `updated: <date>`.

Topic lines for the other pages:

| File | Topic line |
|---|---|
| `architecture.md` | Services, the gateway, the web app, the database per service, docker-compose ports and dependencies, plus a mermaid diagram |
| `tech-stack.md` | .NET version and NuGet packages per project, what each library is used for |
| `adrs.md` | Architecture decisions visible in the code (vertical slices vs. Clean Architecture, CQRS/MediatR, cache decorator, Marten vs. EF), each with: context, decision, consequences |
| `business-logic.md` | How the basket is priced and discounted, the checkout steps, the rules on the Order aggregate, domain events and when they fire |
| `tech-debt.md` | Known issues backed by evidence: missing tests, hardcoded config, no retries or idempotency, unconsumed events. Each one with severity and where it is |

**Step B. Review it yourself.** Open the file and spot-check 2-3 of the
`path:line` references. This is your quality gate.

**Step C. Load it into gbrain:**

```
! gbrain put projects/eshop/interactions --source-id default < docs/kb/interactions.md
! gbrain tag projects/eshop/interactions eshop
```

**Step D. Check that it can be found:**

```
! gbrain search "basket checkout event ordering" --source-id default
! gbrain get projects/eshop/interactions
```

Pass when your page comes back first.

**Step E.** Once all 6 pages are done, link them so they can be navigated:

```
! gbrain link projects/eshop/architecture projects/eshop/interactions
```

Do the same for the other pairs. Then commit `docs/kb/`.

**When the code changes later:** edit the `.md` file, then run `gbrain put`
again. It replaces the whole page.

---

## Phase 3: Memory test across sessions (~20 min)

**Session A** (current session):

> Investigate how Basket and Discount interact when a basket is stored. Save
> your findings to gbrain as page `projects/eshop/flows/basket-discount` in
> source `default`, with `path:line` references.

Then check that the page exists:
`! gbrain get projects/eshop/flows/basket-discount`

**Start a new session** with `/clear`, or close and reopen Claude Code.

**Session B:**

> How does Basket get discount prices? Answer from what you already know about
> this project; don't analyze the code again unless you have to.

**Score it.** Look at Claude's tool calls in the transcript:

- ✅ **Pass:** Claude calls `search`, `query` or `get_page` and answers
  correctly, with **no** Read/Grep of `.cs` files.
- ❌ **Fail:** Claude goes straight to Grep/Read. In that case, check
  `gbrain search "basket discount" --source-id default` yourself: is the page
  there?

**Also test a fact:**

1. Session A: "Remember: we keep Discount on gRPC because it's called once per
   basket item."
2. Session B: "Why is Discount on gRPC?" Claude should answer through `recall`.

**Save the results** in `docs/kb/_verification/phase3-memory-test.md`: the
prompt, the tools Claude used, pass/fail.

---

## Phase 4: gstack skills (one at a time)

| Order | What you do | Pass |
|---|---|---|
| 1 | Ask Claude: "Add an xUnit test project `Basket.API.Tests` with tests for `StoreBasketCommandHandler.DeductDiscount`, with the gRPC client mocked." The repo has no tests, so `/verify` has nothing to run | `dotnet test` is green |
| 2 | `/verify` | Build and tests are green. Then break a test on purpose and run it again: it must go red |
| 3 | `git checkout -b test/review-check`. Plant a bug yourself, e.g. apply the discount twice. Commit, then run `/review` | `/review` finds the bug |
| 4 | Start Docker Desktop, then `docker compose -f src/docker-compose.yml -f src/docker-compose.override.yml up -d`, then `/qa` | It completes the catalog → basket → checkout → order flow. Check that any bug it reports is real |
| 5 | Later, adding each one only after the previous one has run cleanly twice: `/ship`, `/health`, `/cso`, `/retro` | — |

**Save results:** gstack keeps its own reports under `~/.gstack/`. Write a
one-line verdict for each skill in `docs/kb/_verification/phase4-skills.md`.

---

## Phase 5: Full loop on one real feature

Run these in order and keep the knowledge base up to date as you go:

```
/spec "minimum order amount at checkout"
→ ask Claude to implement it ("check docs/kb first")
→ /verify → /review → /qa → /ship
→ update docs/kb/business-logic.md → gbrain put … → /sync-gbrain
```

Track two things: how often Claude rereads code that the knowledge base
already explains, and bugs caught before the PR.

---

## Reusing this on another project

1. `/setup-gbrain`, then `/sync-gbrain --full`
2. Copy the `docs/kb/` folder layout and the six Phase 2 prompts (change the topics)
3. Use the page slugs `projects/<name>/...`
4. Rerun the Phase 1 checks and the Phase 3 test
