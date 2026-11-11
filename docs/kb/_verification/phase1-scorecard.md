# Phase 1 scorecard

- Date:
- gbrain version: (`gbrain --version`)
- Commit: (`git rev-parse --short HEAD`)
- Raw outputs: `docs/kb/_verification/raw/`

Run each command by hand and fill the tables below. To keep a raw output:
`gbrain search "..." --limit 5 > docs/kb/_verification/raw/search-01.txt`

## 1.1 Sync

| Command | Result row (OK / WARN / ERR) |
|---|---|
| `/sync-gbrain --full` | |
| `/sync-gbrain --dream` | |

## 1.2 Definitions (`gbrain code-def <Symbol>`)

| Symbol | Result | Location |
|---|---|---|
| CachedBasketRepository | | |
| DiscountService | | |
| ApplicationDbContext | | |
| IBasketRepository | | |
| Order | | |
| StoreBasketCommand (record, expect MISS) | | |

## 1.3 Call graph (`gbrain code-callers <method>` / `code-callees <method>`)

| Query | Expected | Result |
|---|---|---|
| `code-callees DeductDiscount` | GetDiscountAsync | |
| `code-callers Publish` | CheckoutBasketCommandHandler.Handle | |
| `code-callers CreateNewOrder` | CreateOrderHandler.Handle | |
| `code-callers GetBasket` | CheckoutBasketCommandHandler.Handle, GetBasketQueryHandler.Handle | |

## 1.4 Search (`gbrain search "<question>" --limit 5`)

| Question | Expected file | Rank (1-5 or MISS) |
|---|---|---|
| where is the discount applied to the basket price | StoreBasketHandler.cs | |
| how is the shopping basket cached in redis | CachedBasketRepository.cs | |
| | | |

## Summary

| Area | Score | Target | Pass? |
|---|---|---|---|
| code-def (classes/interfaces) | /  | >= 90% | |
| call edges | /  | >= 80% | |
| search top 5 | /  | >= 80% | |

Known limitations (not counted as failures): C# records are not indexed;
call edges are `resolved=false`; calls between services (gRPC, RabbitMQ)
are not in the graph.
