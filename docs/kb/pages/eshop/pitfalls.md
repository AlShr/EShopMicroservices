---
title: Known pitfalls
tags: [eshop, kb, pitfalls]
updated: 2026-11-12
---

# Known pitfalls

Things in EShop that behave differently from what the code, its comments or
its names suggest. Each one was checked against the code; items marked
**not runtime-tested** follow from reading the code but weren't run.

The first four are also saved in gbrain as facts (code source), so Claude can
recall them without re-reading the code:
`gbrain recall eshop-ordering-service` and `gbrain recall eshop-basket-service`.
Gateway-specific pitfalls are on the [gateway routes](gateway-routes.md) page.

All paths are relative to the repo root.

## Saved as gbrain facts

| # | Pitfall | Evidence | gbrain fact |
|---|---|---|---|
| 1 | **Orders ignore the basket contents.** `BasketCheckoutEvent` carries no item list, and the consumer creates every order with the same 2 hardcoded items. | `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Integration/BasketCheckoutEventHandler.cs:34-38`; event fields `src/BuildingBlocks/BuildingBlocks.Messaging/Events/BasketCheckoutEvent.cs:4-22` | #4 |
| 2 | **`OrderFullfilment` is off locally but on in Docker.** When on, Ordering publishes each created order as `OrderDto` to RabbitMQ (nothing consumes it). | `false` in `src/Services/Ordering/Ordering.API/appsettings.json:11`; `true` in `src/docker-compose.override.yml:114`; used in `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Domain/OrderCreatedEventHandler.cs:13-17` | #3 |
| 3 | **Basket and Ordering validators never run.** Only Catalog registers its FluentValidation validators, so `ValidationBehavior` finds none in the other services. **Not runtime-tested.** | `src/Services/Catalog/Catalog.API/Program.cs:14` is the only `AddValidatorsFromAssembly`; validators exist e.g. in `src/Services/Basket/Basket.API/Basket/CheckoutBasket/CheckoutBasketHandler.cs:11-19` | #5 |
| 4 | **The cache TODO is stale.** `DeleteBasketHandler` says "TODO: delete basket from database and cache", but deleting does clear Redis: the repository is decorated by `CachedBasketRepository`. gbrain search and `think` were both misled by this comment. | TODO at `src/Services/Basket/Basket.API/Basket/DeleteBasket/DeleteBasketHandler.cs:19`; decorator `src/Services/Basket/Basket.API/Program.cs:27-28`; cache removal `src/Services/Basket/Basket.API/Data/CachedBasketRepository.cs:30-37` | #6 |

## Basket

- **Discounts are subtracted again on every save.** `StoreBasket` calls
  `DeductDiscount` before saving (`src/Services/Basket/Basket.API/Basket/StoreBasket/StoreBasketHandler.cs:23-25`),
  and it lowers each item's price by the coupon amount (`:33-37`). The web app
  loads the stored, already-discounted basket and stores it again when adding a
  product (`src/WebApps/Shopping.Web/Pages/Index.cshtml.cs:19-29`), so items
  already in the basket get discounted again. **Not runtime-tested.**
- **One gRPC call per basket item.** `DeductDiscount` calls Discount once for
  each item (`StoreBasketHandler.cs:33-35`). Any failed call fails the whole save.
- **Checkout of a missing basket returns 404, not `IsSuccess = false`.** The
  null check in `src/Services/Basket/Basket.API/Basket/CheckoutBasket/CheckoutBasketHandler.cs:33-36`
  never runs, because `BasketRepository.GetBasket` throws `BasketNotFoundException`
  instead of returning null (`src/Services/Basket/Basket.API/Data/BasketRepository.cs:10`).
- **Checkout publishes, then deletes, with no outbox.** If the delete fails after
  the publish (`CheckoutBasketHandler.cs:41-43`), a retry publishes the checkout again.
- **The Discount gRPC client accepts any TLS certificate**
  (`DangerousAcceptAnyServerCertificateValidator`, `src/Services/Basket/Basket.API/Program.cs:46`).

## Messaging and Ordering

- **Integration event IDs change on every read.** `IntegrationEvent.Id` and
  `OccurredOn` are computed properties (`=> Guid.NewGuid()`, `=> DateTime.Now`,
  `src/BuildingBlocks/BuildingBlocks.Messaging/Events/IntegrationEvent.cs:4-5`),
  so consumers can't de-duplicate messages by ID.
- **Domain events are dispatched before the save commits.**
  `DispatchDomainEventsInterceptor` publishes them in `SavingChanges`, before the
  database write (`src/Services/Ordering/Ordering.Infrastructure/Data/Interceptors/DispatchDomainEventsInterceptor.cs:14-18`).
  With the flag on, `OrderDto` can reach RabbitMQ for an order that then fails to save.
- **Card number and CVV travel in plain text** in `BasketCheckoutEvent`
  (`BasketCheckoutEvent.cs:18-21`) and are stored in the Order table.
- **Audit fields always say `mehmet`.** `AuditableEntityInterceptor` hardcodes
  the user (`src/Services/Ordering/Ordering.Infrastructure/Data/Interceptors/AuditableEntityInterceptor.cs:27,33`).

## Discount

- **Migrations aren't awaited.** `UseMigration` calls `MigrateAsync()` without
  awaiting it and disposes the scope right away
  (`src/Services/Discount/Discount.Grpc/Data/Extentions.cs:9-11`). **Not runtime-tested.**
- **Coupons are matched by product name, not ID**
  (`src/Services/Discount/Discount.Grpc/Services/DiscountService.cs:15-17`).
  Renaming a product in Catalog silently removes its discount.
- **The SQLite database files are committed to git:**
  `src/Services/Discount/Discount.Grpc/discountdb`, `discountdb-shm`, `discountdb-wal`.

## Web app

- **The user and customer are hardcoded:** user name `swn`
  (`src/WebApps/Shopping.Web/Services/IBasketService.cs:22`) and customer ID
  `58c49479-…` (`src/WebApps/Shopping.Web/Pages/Checkout.cshtml.cs:25`).
