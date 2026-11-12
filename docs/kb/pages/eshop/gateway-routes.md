---
title: API gateway routes (YARP)
tags: [eshop, kb, gateway]
updated: 2026-11-12
---

# API gateway routes (YARP)

The web app reaches the backend services only through the YARP gateway
(`src/ApiGateways/YarpGateway`). The gateway has three routes, one per service.
Each route matches a service prefix, strips it, and forwards the rest of the
path unchanged. Discount isn't routed: it's reached only by Basket, over gRPC.

All paths below are relative to the repo root.

## Routes

Defined in config, loaded at `src/ApiGateways/YarpGateway/Program.cs:6`.

| Route | Matches | Forwards to (Docker) | Rate limited | Config |
|---|---|---|---|---|
| `catalog-route` | `/catalog-service/{**catch-all}` | `http://catalog.api:8080` | No | `src/ApiGateways/YarpGateway/appsettings.json:11-17`, cluster `:35-41` |
| `basket-route` | `/basket-service/{**catch-all}` | `http://basket.api:8080` | No | `appsettings.json:18-24`, cluster `:42-48` |
| `ordering-route` | `/ordering-service/{**catch-all}` | `http://ordering.api:8080` | **Yes**, policy `fixed` (`:27`) | `appsettings.json:25-32`, cluster `:49-55` |

Each route has the transform `"PathPattern": "{**catch-all}"` (for example
`appsettings.json:16`), which drops the prefix:

```
GET /ordering-service/orders/customer/58c4…   →   GET http://ordering.api:8080/orders/customer/58c4…
```

Because the whole rest of the path is forwarded, any path a service maps is
reachable through the gateway, not only the endpoints listed below (for
example each service's `/health`).

## Public endpoints

### Catalog (`/catalog-service`)

| Method | Through the gateway | Defined at |
|---|---|---|
| GET | `/catalog-service/products?pageNumber=&pageSize=` | `src/Services/Catalog/Catalog.API/Products/GetProducts/GetProductsEndpoint.cs:10` |
| GET | `/catalog-service/products/{id}` | `…/Products/GetProductById/GetProductByIdEndpoint.cs:11` |
| GET | `/catalog-service/products/category/{category}` | `…/Products/GetProductByCategory/GetProductByCategoryEndpoint.cs:11` |
| POST | `/catalog-service/products` | `…/Products/CreateProduct/CreateProductEndpoint.cs:11` |
| PUT | `/catalog-service/products` | `…/Products/UpdateProduct/UpdateProductEndpoint.cs:11` |
| DELETE | `/catalog-service/products/{id}` | `…/Products/DeleteProduct/DeleteProductEndpoint.cs:11` |

### Basket (`/basket-service`)

| Method | Through the gateway | Defined at |
|---|---|---|
| GET | `/basket-service/basket/{userName}` | `src/Services/Basket/Basket.API/Basket/GetBasket/GetBasketEndpoints.cs:10` |
| POST | `/basket-service/basket` | `…/Basket/StoreBasket/StoreBasketEndpoints.cs:10` |
| POST | `/basket-service/basket/checkout` | `…/Basket/CheckoutBasket/CheckoutBasketEndpoints.cs:10` |
| DELETE | `/basket-service/basket/{userName}` | `…/Basket/DeleteBasket/DeleteBasketEndpoints.cs:10` |

### Ordering (`/ordering-service`, rate limited)

| Method | Through the gateway | Defined at |
|---|---|---|
| GET | `/ordering-service/orders?pageIndex=&pageSize=` | `src/Services/Ordering/Ordering.API/Endpoints/GetOrders.cs:17` |
| GET | `/ordering-service/orders/{orderName}` | `…/Endpoints/GetOrdersByName.cs:16` |
| GET | `/ordering-service/orders/customer/{customerId}` | `…/Endpoints/GetOrdersByCustomer.cs:16` |
| POST | `/ordering-service/orders` | `…/Endpoints/CreateOrder.cs:17` |
| PUT | `/ordering-service/orders` | `…/Endpoints/UpdateOrder.cs:17` |
| DELETE | `/ordering-service/orders/{id}` | `…/Endpoints/DeleteOrder.cs:17` |

## Rate limit on the ordering route

`src/ApiGateways/YarpGateway/Program.cs:8-15` defines one fixed-window limiter
named `fixed`: **5 requests per 10 seconds**. It's switched on at `:20`
(`app.UseRateLimiter()`) and attached only to `ordering-route`.

The limiter isn't partitioned by client or user, so the 5 requests per window
are **shared by all callers** of the ordering routes. Requests over the limit
are rejected; with no `RejectionStatusCode` set, ASP.NET Core's default applies
(503). **UNVERIFIED** at runtime.

## Addresses and ports

| Where | Gateway address | Set at |
|---|---|---|
| Docker, from other containers | `http://yarapipgateway:8080` | `src/docker-compose.override.yml:146` (Shopping.Web's `ApiSettings__GatewayAddress`) |
| Docker, from the host | `http://localhost:6004`, `https://localhost:6064` | `src/docker-compose.override.yml:134-136` |
| Shopping.Web run outside Docker | `https://localhost:6064` (the Docker gateway's HTTPS port) | `src/WebApps/Shopping.Web/appsettings.json:10` |
| Gateway run outside Docker (`dotnet run`) | `http://localhost:5004`, `https://localhost:5054` | `src/ApiGateways/YarpGateway/Properties/launchSettings.json:10,19` |

Shopping.Web calls the gateway through three Refit clients that share the
`ApiSettings:GatewayAddress` base address (`src/WebApps/Shopping.Web/Program.cs:7-23`).

## Pitfalls

- **The gateway only works inside Docker as configured.** `appsettings.json`
  points the clusters at Docker host names (`catalog.api`, …). The localhost
  addresses for running everything on the host are in
  `src/ApiGateways/YarpGateway/appsettings.Local.json:38,45,52`
  (`http://localhost:6000`, `6001`, `6003`), but every launch profile sets
  `ASPNETCORE_ENVIRONMENT=Development` (`launchSettings.json:7,16,25`), so that
  file is never loaded unless you set the environment to `Local` yourself.
- **Two gateway projects.** `src/ApiGateways/YarpGateway` is the one in use: it's
  in the solution (`src/eshop-microservices.sln:40`) and in docker-compose
  (`src/docker-compose.yml:47`). `src/ApiGateways/YarpApiGateway` has the same
  routes but isn't referenced anywhere, so it looks like a leftover. Change
  `YarpGateway`.
- **The Docker service name is misspelled:** `yarapipgateway`
  (`src/docker-compose.yml:43`). Use that exact name in container-to-container
  URLs.
- **Paging is inconsistent.** Catalog pages start at 1 (`pageNumber`, default 1,
  `GetProductsEndpoint.cs:3`); Ordering pages start at 0 (`pageIndex`, default 0,
  `src/BuildingBlocks/BuildingBlocks/Pagination/PaginationRequest.cs:2`).
  Shopping.Web's `GetOrders` defaults to `pageIndex=1`
  (`src/WebApps/Shopping.Web/Services/IOrderingService.cs:7-8`), so it would skip
  the first page of orders. The web app doesn't call it today (the order list
  uses `GetOrdersByCustomer`).
- **Discount has no route.** Its gRPC API isn't exposed through the gateway;
  only Basket calls it.
