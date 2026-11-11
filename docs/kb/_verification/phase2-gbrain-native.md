---
title: Phase 2 evaluation - knowledge base built with gbrain commands
tags: [eshop, kb, verification]
updated: 2026-11-11
---

# Phase 2: knowledge base built with gbrain's own commands

The goal is to measure what **gbrain's own commands** produce on this
project: indexing, synthesis, linking, keeping answers current, and export.
Claude doesn't write the pages. This replaces the prompt-based draft step in
`RUNBOOK.md` Phase 2 for this evaluation.

`think` and `questions pin` use **gbrain's own configured LLM** (they're paid
calls). That isn't Claude prompting: what you're testing is gbrain's pipeline
(retrieval, citations, the link graph, keeping answers current). These two
commands need a chat model key, not only the embedding key; Stage 0 checks
that.

All commands below come from the help output of gbrain 0.60.645.0.

## Method

Ask the same fixed questions at every stage. Score each answer:

| Score | Meaning |
|---|---|
| 0 | Wrong or missing |
| 1 | Partly right, or right without a usable citation |
| 2 | Correct, with a `path:line` citation that checks out |

The score change between stages is the benefit you're measuring.

### Fixed questions

| # | Question |
|---|---|
| Q1 | Which services exist and which database does each use? |
| Q2 | How does Basket get discounts? |
| Q3 | What happens when a basket is checked out? |
| Q4 | Who consumes `BasketCheckoutEvent`? |
| Q5 | How is a basket cached? |
| Q6 | Which routes does the API gateway expose? |
| Q7 | What is published to RabbitMQ, and by whom? |
| Q8 | Which feature flags exist and what do they control? |

## Flow

| Stage | gbrain commands | What it tests | Record |
|---|---|---|---|
| **0. Readiness** | `gbrain stats --by source,type`, `gbrain sources status`, `gbrain health`, `gbrain features`, then one test: `gbrain think "What services exist?"` | That the brain is healthy and `think` has a chat model | Page counts, embedding coverage, whether `think` works |
| **1. Baseline: search only** | `gbrain query "<question>"` for each question | What the raw code index answers with no knowledge base | Score per question (top 5 hits) |
| **2. gbrain writes pages** | `gbrain think "<question>"` (preview), then `gbrain think "<question>" --save` for good answers | Can gbrain write a cited knowledge base page from code by itself? Pages go to `synthesis/<slug>-<date>` | Score, citation accuracy (open 3 `path:line` refs per answer), cost per question |
| **3. Answers that stay current** | `gbrain questions pin "<question>"` for 3-5 key questions, then `gbrain questions list` and `gbrain questions status <id>` | The main benefit beyond a static `.md`: gbrain marks answers stale when the code changes | Answer score now; later, change a file, run `/sync-gbrain`, check that `status` flags the right sentences |
| **4. Structure** | `gbrain extract all --source db --source-id default --dry-run`, then without `--dry-run`; `gbrain orphans`; `gbrain graph <slug> --depth 2`; `gbrain check-backlinks check` | Can gbrain link pages automatically, without manual `gbrain link`? | Links created, orphan count before and after |
| **5. Into git** | `gbrain export --source default --dir docs/kb/generated` | How gbrain's pages become `.md` files in the repo | Whether the files are readable for a new developer as they are |
| **6. Feeding Claude Code** | `gbrain compile-context --target claude-code --stdout` | What a new Claude Code session would get from the brain automatically | Tokens used, whether it covers Q1-Q8 |
| **7. Upkeep** | `gbrain dream --dry-run`, `gbrain extract --stale --dry-run` | How much work it takes to keep the knowledge base current | What it would change |
| **8. Verdict** | Repeat Stage 1 `query` for all questions | The before/after difference, now with the gbrain-built pages | Final scorecard |

## Cautions

- **Stage 5:** `export` never overwrites. Use a fresh `--dir` each run or it
  will refuse.
- **Stage 6:** without `--stdout`, `compile-context` writes
  `.claude/gbrain-context.md` and changes `CLAUDE.md`. Look at the output with
  `--stdout` first.
- **Source scope:** in this repo, commands default to the code source
  (`.gbrain-source`). Pages saved by `think` and pins go to whichever source is
  in effect. Check with `gbrain stats --by source,type` after Stage 2.
- **`reconcile-links`** (links docs to the code they describe) has no help
  text. Run it with `--dry-run` first if you include it.
- **The smoke-test page** `setup-gbrain-smoke-test-1794308151` in `default`
  adds noise to Stages 1 and 8. Delete it first for clean scores:
  `gbrain delete setup-gbrain-smoke-test-1794308151`.

## Results

### Stage 0: readiness

| Pages per source and type | 195 total. Code: 177 code + 1 concept. `default`: 3 learning, 3 timeline, 1 concept (setup pages, no KB yet). Transcripts: 9 (2 sources). Artifacts: 1 |
| Embedding coverage | Code 100% (indexed through 9b924da). `default` 14% (6/7 waiting), transcripts-1d12c9 55%; others 100%. Ran `gbrain embed --stale` before Stage 1: <result> |
| Sync state | `default` 100% embedded but 7 files behind its git HEAD (be9682de vs. indexed 798bfc22); not synced before Stage 1 to keep the baseline free of unknown content. Code index current through 9b924da. Backfill jobs queued, no worker |
| `health` | 6/10. Embed 82.7% of chunks (225 missing, all in un-embedded transcripts); 8 stale pages (7 in `default` + 1 code); 195/195 orphans; 0 entity pages, so link and timeline scores are n/a. The score is tuned for personal brains (people, timelines) and underrates a code KB |
| `think` works | Yes. Model openai:gpt-5.6-sol; 40 pages read, 13 citations, Graph 0, $0.0676 per question, no approval prompt (unlike `embed`). Test answer: 4 services, web app and containers correct; **missed the YARP gateway** and Discount's SQLite; flagged its own gap (docker-compose excerpts cut off). Citations are page slugs (file level), not path:line |


### Scores per stage

| Question | S1 `query` | S2 `think` | S3 pinned | S8 `query` again |
|---|---|---|---|---|
- Q1: top 5 had docker-compose.yml, docker-compose.override.yml and Basket appsettings
  (enough to answer after reading them); Program.cs DB registrations (AddMarten,
  UseSqlite, UseSqlServer) not in top 25. Noise: GlobalUsing.cs chunks (5 of 25 hits).
  The tool hint suggested person/company type filters, which doesn't fit a code brain.
  Notes under Stage 1:
- Q2: #1 was the exact answer, `StoreBasketHandler.cs:30-38` (DeductDiscount). "Basket"
  outweighs "discounts": hits 3-5 are unrelated Basket files; the server side
  (DiscountService.GetDiscount) is #16. discount.proto not in top 25 (check whether .proto is indexed).
- Q3: 4 of top 5 = the whole cross-service flow (web client → Basket endpoint →
  CheckoutBasketHandler:21-47 → Ordering BasketCheckoutEventHandler). Semantic search
  links Basket and Ordering, which the call graph can't (Publish unresolved). Not in
  top 5: event payload (#20), CreateOrderHandler (not in top 25). The hardcoded order
  items are only visible by opening hit #4.
- Q4: #1 = the consumer (Ordering BasketCheckoutEventHandler, score 0.90, the highest so far):
  an exact type name ranks much better than a general description. A ranked list
  can't prove there's only one consumer; confirm with `gbrain code-refs BasketCheckoutEvent`.
  CLI rows are cut at ~150 characters, so long Ordering paths lose their `:line`.
- Q5: #1 (0.82) was DeleteBasketHandler, ranked by a stale comment
  `// TODO: delete basket from database and cache` (line 19); no cache code. The real
  answer, CachedBasketRepository.GetBasket:10-19, was #2 (0.57). Missing from top 25:
  cache write/delete (:21-37) and the wiring (Program.cs:27-34, Scrutor decorator + Redis).
  Comments rank like code; wording mismatch ("cached" vs. CachedBasketRepository /
  IDistributedCache) gives weak scores.
- Q5 variant, using identifiers ("CachedBasketRepository IDistributedCache Redis"): file
  goes to #1 (0.89 vs. 0.57), Redis config at #4/#6. But the chunk is the `using` line
  (:1-1), not the methods: "right file, wrong chunk" recurs (Q4, Q5).
- query works well when you know the identifiers; a new developer doesn't. That's the gap
  the KB stages should fill.
- Program.cs only ever appears as its `using` chunk (:1-4); the top-level DI/wiring code
  (AddMarten, AddStackExchangeRedisCache, Decorate, AddGrpcClient) never shows up. Check:
  `gbrain search "AddStackExchangeRedisCache"`.
- Q5 with --expand: #1 = CachedBasketRepository.StoreBasket :21-28 (0.84), real cache
  code; the TODO comment hit drops to #2. Expansion fixes the wording mismatch but raises
  every score (0.58-0.84 vs. 0.38-0.82), so expanded and plain scores aren't comparable.
  One LLM call, no cost shown, no approval prompt. Program.cs wiring still missing.
- Q6 with `search` (keyword) instead of `query`: the gateway answer
  (YarpGateway/appsettings.json:1-58) at #5. #1-4 are Basket `AddRoutes`
  methods matched by the word "routes". Gateway Program.cs not shown;
  duplicate YarpApiGateway project not in top 20. Hint warns that search
  count is not proof of completeness. Score Q6 from the `query` run.
- Q7: miss. Top 5 = broker config (AddMessageBroker, compose, appsettings). Publisher
  CheckoutBasketHandler:25-46 at #10; Ordering's OrderCreatedEventHandler (2nd publisher)
  not in top 25. "RabbitMQ" matches config, not code that calls Publish. By contrast,
  `gbrain code-callers Publish` returned all 3 publishers in one call: structural commands
  win for "who does X", if you know the method name.
- Noise: wwwroot/lib/jquery-validation/LICENSE indexed (#6), so vendored third-party
  files are in the code index (likely the 1 "concept" page in the code source).
  - Q8: answer split over #3 (OrderCreatedEventHandler, what the flag controls, :line cut
  off) and #5 (Ordering appsettings.json, OrderFullfilment=false). The Docker override
  that sets it true (docker-compose.override.yml:114) not in top 25, so a reader would
  think the flag is off. launchSettings.json files take 8 of the top 13 hits (noise).
  The leftover YarpApiGateway project is indexed (#8, #13, #15, #20).
  
### Stage 2: citation accuracy and cost

| Question | Citations checked | Correct | Cost | Saved page slug |
|---|---|---|---|---|
- Q1: all claims correct, 10/10 citations point to the right files. Discount's DB
  reported as unknown (it's SQLite, set in Program.cs:9-10, a top-level statement;
  more evidence that Program.cs isn't indexed). Gateway missing: it used the cut-off
  docker-compose.yml, not the override. Direct answer table instead of 5 files to read;
  same score as S1 under the strict rubric.
  - `put` of a think answer: committed to `default` as 1 chunk; auto_links created 0:
  think's `[slug]` citations aren't link syntax, so saved pages stay orphans with
  no links to the code they cite. embedding_state queued (no worker live), so
  embed `default` before Stage 8.
  
- Q2: all claims correct (StoreBasketHandler.cs:33-37, DiscountService.cs:15-20,
  local and Docker addresses). gbrain flagged a model typo in one citation
  (CITATION_NOT_IN_GATHER): citations are checked against the evidence read. Missing:
  that discounts are applied on StoreBasket before saving. No Gaps section.
  
  - Q3: think scored LOWER than query (1 vs. 2). All 6 claims correct, but it missed
  the core: publish BasketCheckoutEvent, then delete the basket
  (CheckoutBasketHandler.cs:32-43), and Ordering's hardcoded order items. Its Gaps
  section says method bodies weren't in its evidence: it got class outlines with
  member names, not code. Good at "what exists and how it connects", weak at
  "what happens step by step".

- Q4: correct and minimal (BasketCheckoutEventHandler in Ordering, Consume +
  MapToCreateOrderCommand). Cheapest answer so far ($0.03): an exact type name
  gives a short, exact answer. Doesn't show that it's the only consumer, and
  doesn't explain the consumer registration (AddMessageBroker with assembly).
  
  - Q5: read path (CachedBasketRepository.cs:10-19) and Redis config correct. Misled by the
  stale TODO in DeleteBasketHandler.cs:19: says cache invalidation on delete "is not
  demonstrated", but CachedBasketRepository.DeleteBasket removes it (:30-37). Gaps list
  store-refreshes-cache (it does, :21-28; the method-body blind spot) and registration
  (Program.cs:27-34; the Program.cs gap, 3rd time). Stale comments mislead both query and think.
  
  - Q7: found Basket → BasketCheckoutEvent (CheckoutBasketHandler.cs:38-41), but missed
  Ordering → OrderDto (OrderCreatedEventHandler.cs:13-16), and the Gaps section doesn't
  say other publishers might exist (a silent omission). It described the publish from
  the handler's comments (:27-30), not the Publish call (method body not in evidence).
  `gbrain code-callers Publish` (free) found all 3 Publish calls: a structural lookup
  beats think on "who does X".
  
  - Q8: found OrderFullfilment=false (Ordering appsettings.json:10-11), noted the spelling,
  and that it's wired into Ordering. Couldn't say what it controls (OrderCreatedEventHandler
  body not in evidence; it gates publishing OrderDto, :13-17). Docker sets it true
  (docker-compose.override.yml:114): not found, but flagged as a possible gap.
- Stage 2 summary: S2 total 11/16. Up: Q6 (synthesis over 16 files), Q7. Down: Q3
  (method bodies). Patterns: method-body blind spot (Q3/5/7/8), Program.cs gap (Q1/5/6),
  honest Gaps with no made-up facts (except a silent omission in Q7 and a stale-comment
  hedge in Q5). Cost: $0.43 for 8 answers.
  
  
!  gbrain think "What is published to RabbitMQ, and by whom?" --source gstack-code-6651f992-3f84fc > docs/kb/_verification/think/q7.md  !  cat docs/kb/_verification/think/q7.md

!  cat docs/kb/_verification/think/q7.md
!  gbrain put synthesis/eshop-q7-rabbitmq --source-id default < docs/kb/_verification/think/q7.md | grep -E '^  "(slug|state|embedding_state)"'

for last
! gbrain embed --stale --yes --source default


### Stage 3: freshness test

| Pin ID | Question | File changed | Stale sentences flagged correctly? |
|---|---|---|---|
- Q2 pinned in the code source (pins can write there; think --save couldn't).
  4 sentences, all correct, and better than think: says discounts are applied
  when the basket is stored, before saving. No cost shown, no approval asked.

- Q3 pin: 5/5 sentences correct, the full sequence (page → validate → recalculated total →
  publish → Ordering create-order → delete from DB and cache → HTTP 200). Much better than
  think (1): pins read method bodies; think didn't. Also corrects think's Q5 claim about
  cache delete. Caveats: "validates" describes the code, but Basket never registers its
  validators; the hardcoded order items aren't mentioned.
  
  - Q8 pin: correct on both parts: OrderFullfilment=false, and when on, it publishes the
  created order as an integration event (OrderCreatedEventHandler.cs:13-17), which
  think couldn't establish. Missing: Docker sets it true (docker-compose.override.yml:114).
- Pins vs. think so far: Q2 2/2 (pin adds timing), Q3 1→2, Q8 1→2. Pins read method
  bodies; think doesn't.
  
  - Sync after the flip (9b924da → f7a39e7): +12 added (the docs/kb .md files),
  ~0 modified: check that appsettings.json was re-indexed. Side effect: the code
  source indexes all committed files, so the evaluation notes and think answers are
  now in the code index (use `query --types code` in Stage 8). The live `gbrain serve`
  is the writer coordinator, which explains the earlier think --save error.
  
  
  - Freshness test PASSED: after flipping OrderFullfilment and `sync --strategy code`, only
  the Q8 pin went stale (1 of 3 sentences); Q2 and Q3 (controls) stayed fresh. Per-sentence
  staleness detection works with no false alarms.
- Detection is automatic, re-answering isn't: all pins `blocked: no_worker`. Updating needs a
  manual `questions refresh` or `gbrain autopilot --install`.
  
    (sentence 1). The other 2 sentences, which the change didn't affect, stayed fresh:
  per-sentence precision. (status prints to stderr; use 2>&1 when piping.)
  
  
  - `questions refresh` on the stale Q8 pin FAILED: the new answer still says
  OrderFullfilment is `false` (gbrain's appsettings.json page says `true`) and all
  sentences show fresh. Likely cause: docs/kb/_verification (think/q8.md, notes saying
  `false`) was indexed into the code source by the earlier sync, and the refresh used it
  as evidence. "Fresh" means consistent with retrieved evidence, not correct. Keep KB and
  evaluation docs out of the source that pins read.
- Freshness test summary: detection passed (right sentence, no false alarms);
  re-answering failed (stale fact regenerated, alert cleared).

- Cleanup sync (`--strategy code`, f7a39e7 → dc4e708): appsettings.json back to false.
  Each sync strategy covers only its file types: code strategy skipped the changed .md
  in the same commit; plain sync skips code. A fully current source needs both
  (or /sync-gbrain).
  
  - After the cleanup sync (appsettings back to false), the refreshed Q8 pin stayed
  fresh: its evidence is now docs/kb/_verification/think/q8.md, not appsettings.json,
  so config changes no longer affect it. Its text ("false") is right by coincidence.
  The refresh silently broke change tracking for this pin. Fix: keep docs/ out of the
  code source, then unpin and pin again.
- Stage 3 summary: pin answers best of all stages (3/3 better than think); detection
  precise; refresh and tracking after refresh unreliable when docs share the source;
  automatic refresh needs autopilot.
  
### Stage 4: structure

Stage 4 table, "Before" column:
| Links | 0 (every page is an orphan) | |
| Orphan pages | 195 | |
- `extract all --source db --source-id default --dry-run`: 0 links, 0 timeline
  entries from 15 pages; 9 candidates skipped (target page doesn't exist in this source).
  think's `[slug]` citations point to the code source, so basic extract can't link
  KB pages to code. Orphans unchanged (195).
- `reconcile-links --dry-run`: scanned 41 Markdown pages across all sources (including
  transcripts), found 17 code refs, attempted 0 edges. think answers cite page slugs
  (`[src-…]`), which it doesn't count as code refs (Q6 alone has 18), so gbrain's own
  answer format doesn't feed its docs↔code linker. Unclear whether 0 attempted is
  dry-run behaviour or unmatched refs.
  
  
- `extract links --by-mention`: "No linkable entity pages" (needs person/company/
  organization/entity pages).
- Stage 4 summary: none of gbrain's automatic linking applies to a code KB. extract
  needs same-source explicit links; by-mention needs entity pages; reconcile-links
  doesn't recognise think's slug citations; timeline needs dates. Only manual
  `gbrain link` remains. The graph-building is designed for a personal brain
  (people, meetings, timelines), like the health score.

Stage 4 table:
| Links | 0 | 0 from Stage 4 (1 created earlier by sync, between docs/kb .md files) |
| Orphan pages | 195 of 195 | 216 of ~218 (more pages, still ~99% orphans) |


### Stages 5-7

| Stage | Observation |
|---|---|
| 5. Export readable as-is? | |
- Export of a think answer: body unchanged; gbrain adds frontmatter (type: concept, title
  from the H1, trust_tier: agent_written, ingested_at, source_kind). No tags.
- Readability for a new developer: content good, but `[src-…]` citations stay plain slugs,
  not links or paths, so they can't be followed on GitHub, and mapping a slug back to a file
  is guesswork (case, `/` and `.` are lost). Cost footer stays in. No index, no links between
  pages. Result: a folder of separate Q&A notes, not navigable documentation.




| 6. Context file: tokens, covers Q1-Q8? | 4000-token budget, from the pinned code source (not default). Each entry = title + brain:// link + FIRST LINE only (`{`, `namespace …`, `1.`). Includes the think answers only via their docs/kb copies, plus my evaluation notes, the minified jQuery plugin, CSS and EF migrations; budget ends before Catalog, Basket, Discount and the gateway. Covers Q4 only. Not useful as given; and it would feed evaluation notes into future Claude sessions |! gbrain compile-context --target claude-code --stdout --source default

Ready to paste under Stages 5-7:
| 6b. Context from `--source default` | The 8 KB answers first (newest first), first line only; complete facts only for Q2, Q4, Q7 (half). Then 7 gstack JSON event and learning logs, mostly from OTHER work (gstack/gbrain Windows branches): `default` is shared across all projects, so their notes would enter EShop's context. Far under the 4000-token budget; first-line rendering is the limit. Works as an index (question titles + brain:// links) for Claude to open pages over MCP, not as knowledge. Best setup: the KB in its own source |
| 6. Context file: tokens, covers Q1-Q8? | 4000-token budget, from the pinned code source (not default). Each entry = title + brain:// link + FIRST LINE only (`{`, `namespace …`, `1.`). Includes the think answers only via their docs/kb copies, plus my evaluation notes, the minified jQuery plugin, CSS and EF migrations; budget ends before Catalog, Basket, Discount and the gateway. Covers Q4 only. Not useful as given; and it would feed evaluation notes into future Claude sessions |


| 7. Upkeep: `dream --dry-run` | Half the phases need a local brain dir (Postgres has none). Relevant to a code KB: standing_questions (refreshes pins only when due; 0 due), embed (226 chunks, mostly transcripts: a real run would bring Claude session text into search), resolve_symbol_edges (skipped in dry run; may fix the unresolved call graph, untested). Most other phases are personal-brain features, off by default. unindexed_files: none missing. Orphans 216/218 |
| 7b. `extract --stale --dry-run` | 16 pages need link/timeline extraction, 218 need a mention pass (never run). Stage 4 showed these passes yield 0 links and 0 entries for a code KB, so "stale" never reaches 0: not a usable health signal. Real upkeep for this KB = sync --strategy code (code), plain sync (Markdown), embed, questions refresh / autopilot |

## Verdict

- Benefit over plain code search (S8 vs. S1):
- Where gbrain's own output was good enough to keep:
- Where a human still had to write or fix:
- Cost per question and in total:

- Setup: `query "<Q>" --source-ids gstack-code-6651f992-3f84fc,default` (comma list works).
  Score = best answer available in the top 5; eval notes (phase1/phase2 files) don't count.
- Q1: KB page synthesis/eshop-q1-services #1 (1.098 vs. 0.39 next): a direct answer, but its
  content scored 1. Code hits as in S1. Score 1. Caveat: the question equals the KB page title
  (near-exact match); test a paraphrase. Scores are scaled across sources, so compare ranks, not numbers.
  
  - Q1 paraphrase ("what databases do the microservices use"): KB page still #1 (0.86), but only
  0.05 ahead of docker-compose.override.yml (vs. 0.71 for the exact title). The KB is findable
  without its exact wording; the big lead before was an exact-match bonus. Scores are scaled
  per query, so compare ranks only.
  
  - Q2: #1 = docs/kb/_verification/think/q2 (code-source copy of the KB answer, 1.36); code hit
  StoreBasketHandler.cs:30-38 dropped from #1 (S1) to #3. 4 of the top 5 are docs/kb pages,
  2 of them noise (runbook, eval notes): the docs push the code down. The synthesis/ copy in
  default isn't in the top 25 (apparently one of two identical pages is kept). The Q2 pin,
  the best answer, isn't returned either; check with --explain-target.
  
  - Q3: #1 synthesis/eshop-q3-checkout and #2 think/q3-rounds2 (both near 1.72, both weaker answers
  scored 1); the full CheckoutBasketHandler.cs:21-47 body dropped from #2 (S1) to #5. Score 2 only
  because it's still in the top 5. Weaker KB answers outrank better code: a reader stopping at #1-2
  gets the incomplete answer. Identical copies are de-duplicated (kept copy differs per query);
  near-duplicates both appear. The Q3 pin (best answer) still not returned.
  
  - Q4: #1 think/q4 (KB answer, 2). #2-4 are other KB answers (q3, q3-rounds2, q7); the consumer
  code (S1 #1) dropped to #5. Pattern Q2-Q4: best code hit S1 → S8 = #1→#3, #2→#5, #1→#5.
  KB pages are short and topic-dense, so they rank for any related question; with a larger KB,
  code would fall out of the top 5. Kept duplicate copy varies per query (think/q3 here,
  synthesis in Q3).
  
  - Q5: #1 think/q5 (KB answer, 1, with the misleading cache-delete claim), #2 the stale TODO
  (DeleteBasketHandler.cs:17-23) it was based on. The real cache code (CachedBasketRepository.cs:10-19)
  dropped from #2 (S1) to #7. A KB mistake reinforces itself: wrong answer #1, its misleading source
  #2, the correcting code pushed out. Both eval files in the top 5 (#3, #4). No review step in gbrain
  would catch this.
  
  - Q6: #1 synthesis/eshop-q6-gateway-routes (the complete answer, 16/16 endpoints). Clearest KB win:
  one page replaces combining the gateway config with 16 endpoint files. The topic doesn't overlap
  other KB pages, so #2-5 are code, not other KB answers. The KB helps when pages cover distinct
  topics, and crowds results when they overlap (Q2-Q5).
  
  - Q7: #1 synthesis/eshop-q7-rabbitmq (KB answer, 1); S1 was 0, so the KB page is the only gain.
  #2-5 = eval notes (twice: chunks of one page aren't de-duplicated), runbook, scorecard. The eval
  notes name both publishers and would give a false 2, hence the exclusion rule. Publisher code
  dropped #10 → #22; OrderCreatedEventHandler still not in top 25. 4 of the top 5 are docs about
  gbrain, not EShop: docs/ must be in its own source.
  
  
  ## Verdict

- **Benefit over plain code search (S8 vs. S1):** small. Total 12/16 vs. 9-10/16. Gains only
  where the answer is spread over many files (Q6) or search missed entirely (Q7). On Q2-Q5 the
  KB pages pushed the best code hits down (#1→#3, #2→#5, #1→#5, #2→#7), and on Q5 a wrong KB
  claim now ranks #1 with its misleading source at #2.
- **Best gbrain feature: pinned questions.** Pin answers were the most accurate of any stage
  (Q3: 5/5 sentences, full checkout sequence), and change detection was precise (only the
  affected sentence went stale, no false alarms). But refresh can bring back a stale fact
  from saved answers and mark it fresh, and pins don't appear in `query` results.
- **`think`:** honest (reports gaps, no made-up facts) and good at combining many small files
  (Q6), but it sees class outlines, not method bodies, so it can't explain step-by-step
  behaviour (Q3, Q5, Q7, Q8). Cost: ~$0.03-0.09 per answer.
- **Index gaps:** top-level Program.cs code (all DI wiring) never retrieved; vendored jQuery
  indexed; stale comments rank like code.
- **Linking and graph:** none of gbrain's automatic linking applies to a code KB (it's built
  for people, companies and timelines); ~99% of pages stay orphans; think never used the graph.
- **Export and Claude Code context:** export gives readable Q&A files, but citations are slugs
  you can't follow. compile-context gives titles + first lines only: usable as an index for
  Claude over MCP, not as knowledge.
- **Setup rules learned:** keep docs/ (KB pages, notes) in a separate source from the code,
  or they compete with and contaminate it; code changes need `sync --strategy code`;
  `default` is shared by all projects on the machine.
- **Where a human still had to work:** working around the save errors, checking every answer
  against the code (one misleading claim, one silent omission), and anything needing links,
  an index or followable citations.
- **Cost:** ~$0.43 for 8 think answers, ~$0.15 for pins and tests, ~$0.01 for embeddings.