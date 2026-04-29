# Developer Evaluation Project — Sales API

Implementation of the **Sales API** prototype described in [`/.doc/overview.md`](./.doc/overview.md), following `DDD` + CQRS (MediatR) on top of the provided template (.NET 8 / EF Core / PostgreSQL).

## Structure

```
root
├── src/
│   ├── Ambev.DeveloperEvaluation.Domain         # Entities, business rules, events, validators
│   ├── Ambev.DeveloperEvaluation.Application    # Commands/Queries (MediatR), AutoMapper profiles
│   ├── Ambev.DeveloperEvaluation.ORM            # EF Core, mappings, repositories, migrations
│   ├── Ambev.DeveloperEvaluation.WebApi         # Controllers, requests/responses, middlewares
│   ├── Ambev.DeveloperEvaluation.IoC            # Module initializers / DI
│   └── Ambev.DeveloperEvaluation.Common         # Logging, validation pipeline, security, JWT
├── tests/
│   ├── Ambev.DeveloperEvaluation.Unit           # xUnit + NSubstitute + Bogus
│   ├── Ambev.DeveloperEvaluation.Integration    # SaleRepository + EF InMemory
│   └── Ambev.DeveloperEvaluation.Functional     # WebApplicationFactory<Program> end-to-end
├── docker-compose.yml                            # WebApi / Postgres / Mongo / Redis
├── Dockerfile
└── Ambev.DeveloperEvaluation.sln
```

## What was implemented

### Domain (Sale)
- [`Sale`](src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs) and [`SaleItem`](src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs) following the *External Identities + denormalization* pattern (CustomerId/Name, BranchId/Name, ProductId/Name).
- Discount rules encapsulated in `SaleItem`:
  - `< 4` identical items → **0%**
  - `4–9` identical items → **10%**
  - `10–20` identical items → **20%**
  - `> 20` → throws `DomainException` (forbidden)
- Whole-sale cancellation (`Sale.Cancel`) and per-item cancellation (`Sale.CancelItem`) with automatic `TotalAmount` recalculation.
- FluentValidation validators: [`SaleValidator`](src/Ambev.DeveloperEvaluation.Domain/Validation/SaleValidator.cs), [`SaleItemValidator`](src/Ambev.DeveloperEvaluation.Domain/Validation/SaleItemValidator.cs).
- Domain events: [`SaleCreatedEvent`](src/Ambev.DeveloperEvaluation.Domain/Events/SaleCreatedEvent.cs), `SaleModifiedEvent`, `SaleCancelledEvent`, `ItemCancelledEvent`, dispatched via [`IDomainEventPublisher`](src/Ambev.DeveloperEvaluation.Domain/Events/IDomainEventPublisher.cs).

### Application (CQRS via MediatR)
| Use case | Handler |
|---|---|
| Create sale | [`CreateSaleHandler`](src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs) |
| Get by id | [`GetSaleHandler`](src/Ambev.DeveloperEvaluation.Application/Sales/GetSale/GetSaleHandler.cs) |
| List (pagination/filtering/ordering) | [`ListSalesHandler`](src/Ambev.DeveloperEvaluation.Application/Sales/ListSales/ListSalesHandler.cs) |
| Update | [`UpdateSaleHandler`](src/Ambev.DeveloperEvaluation.Application/Sales/UpdateSale/UpdateSaleHandler.cs) |
| Delete | [`DeleteSaleHandler`](src/Ambev.DeveloperEvaluation.Application/Sales/DeleteSale/DeleteSaleHandler.cs) |
| Cancel sale | [`CancelSaleHandler`](src/Ambev.DeveloperEvaluation.Application/Sales/CancelSale/CancelSaleHandler.cs) |
| Cancel item | [`CancelSaleItemHandler`](src/Ambev.DeveloperEvaluation.Application/Sales/CancelSaleItem/CancelSaleItemHandler.cs) |

Each use case exposes a `Command` / `Handler` / `Validator` (FluentValidation) and DTOs (`SaleResult`, `SaleItemResult`) wired through `AutoMapper`. Handlers that mutate state publish the matching domain event.

### ORM (EF Core / PostgreSQL)
- Mappings: [`SaleConfiguration`](src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleConfiguration.cs) (unique index on `SaleNumber`, `Status`→string conversion, `Items` navigation), [`SaleItemConfiguration`](src/Ambev.DeveloperEvaluation.ORM/Mapping/SaleItemConfiguration.cs).
- [`SaleRepository`](src/Ambev.DeveloperEvaluation.ORM/Repositories/SaleRepository.cs) exposes `ListAsync` with filters (`CustomerId`, `BranchId`, `SaleNumber` / `CustomerName` / `BranchName` with `*` wildcards, `_minSaleDate`/`_maxSaleDate`, `_minTotalAmount`/`_maxTotalAmount`, `IsCancelled`), multi-field ordering (`saleDate`, `saleNumber`, `customerName`, `branchName`, `totalAmount`, `status`, `createdAt` — asc/desc) and pagination.
- Migration `20260429083624_AddSales` creates the `Sales` / `SaleItems` tables and adds `CreatedAt` / `UpdatedAt` to `Users` (gap left by the original migration).

### WebApi
[`SalesController`](src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/SalesController.cs) is decorated with `[Authorize]` — every endpoint requires a JWT bearer token (obtained from `POST /api/auth`). Anonymous calls return **401 Unauthorized**.

It exposes:

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/sales` | Create sale |
| `GET` | `/api/sales/{id}` | Get by id |
| `GET` | `/api/sales` | List with `_page`, `_size`, `_order`, `customerId`, `branchId`, `saleNumber` (`*`), `customerName` (`*`), `branchName` (`*`), `_minSaleDate`, `_maxSaleDate`, `_minTotalAmount`, `_maxTotalAmount`, `isCancelled` |
| `PUT` | `/api/sales/{id}` | Update sale (replaces items) |
| `DELETE` | `/api/sales/{id}` | Delete sale |
| `POST` | `/api/sales/{id}/cancel` | Cancel the entire sale |
| `POST` | `/api/sales/{id}/items/{itemId}/cancel` | Cancel a single item |

- [`GlobalExceptionMiddleware`](src/Ambev.DeveloperEvaluation.WebApi/Middleware/GlobalExceptionMiddleware.cs) and [`ValidationExceptionMiddleware`](src/Ambev.DeveloperEvaluation.WebApi/Middleware/ValidationExceptionMiddleware.cs) translate exceptions into the `{ type, error, detail }` envelope defined in [`/.doc/general-api.md`](.doc/general-api.md): `ValidationException` → 400 (`type: "ValidationError"`), `KeyNotFoundException` → 404 (`type: "ResourceNotFound"`), `DomainException` → 400 (`type: "DomainRuleViolation"`), `InvalidOperationException` → 409 (`type: "Conflict"`), everything else → 500.
- Paginated response: `{ data: [...], totalItems, currentPage, totalPages, success }` ([PaginatedResponse.cs](src/Ambev.DeveloperEvaluation.WebApi/Common/PaginatedResponse.cs)) — aligned with the `/.doc` convention.
- [`LoggingDomainEventPublisher`](src/Ambev.DeveloperEvaluation.WebApi/Events/LoggingDomainEventPublisher.cs) dispatches domain events to Serilog (no real broker — allowed by the assignment).

### Tests (xUnit + NSubstitute + Bogus, **no FluentAssertions**)
- **Unit (72)** — discount/cancellation rules in `SaleTests` / `SaleItemTests`, `CreateSaleHandlerTests`, plus the pre-existing User tests.
- **Integration (6)** — [`SaleRepositoryTests`](tests/Ambev.DeveloperEvaluation.Integration/Sales/SaleRepositoryTests.cs) on EF InMemory, covering create/get/delete + pagination + filters (wildcard on `customerName`, range on `totalAmount`).
- **Functional (8)** — [`SalesEndpointsTests`](tests/Ambev.DeveloperEvaluation.Functional/Sales/SalesEndpointsTests.cs) boots the full API via `WebApplicationFactory<Program>` ([`SalesApiFactory`](tests/Ambev.DeveloperEvaluation.Functional/SalesApiFactory.cs)) and asserts POST/GET/list/cancel/cancel-item + the `{type, error, detail}` envelope on 400/404 + a 401 case for missing token. The factory bootstraps a test user (`POST /api/users`) and logs in (`POST /api/auth`) to inject a real bearer token into the test client.

## How to run

### Prerequisites
- .NET 8 SDK
- Docker (recommended)

### Build
```bash
dotnet build Ambev.DeveloperEvaluation.sln
```

### Tests
```bash
dotnet test Ambev.DeveloperEvaluation.sln
# 86 tests · 0 failures (Unit 72 + Integration 6 + Functional 8)
```

### Run the full stack with Docker Compose (recommended)
```bash
docker compose up -d --build
```

This brings up:
- **Postgres** (port 5432) — database `developer_evaluation`, user `developer`
- **MongoDB** (port 27017) and **Redis** (port 6379) — inherited from the template, not used by the Sales API
- **WebAPI** (port **8080**) — applies pending EF Core migrations on startup

Endpoints:
- API: `http://localhost:8080/api/sales`
- Swagger: `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health`

### Test with Postman
Import the collection [`/.doc/Sales.postman_collection.json`](.doc/Sales.postman_collection.json):

1. Postman → **Import** → select the file
2. The `baseUrl` variable defaults to `http://localhost:8080`
3. Run in order:
   1. `Setup - Register tester user` (idempotent — accepts 201 or 409)
   2. `Setup - Login (gets JWT)` — captures the JWT into the `bearerToken` collection variable
   3. `Create Sale` → `Get Sale by Id` → the rest
   (`Create Sale` writes the new `saleId`, `Get Sale by Id` writes `itemId`)

The collection has bearer auth configured at the collection level using `{{bearerToken}}`, so once the Login is run every subsequent request is authenticated automatically.

The collection ships 17 requests: setup (register+login), full CRUD, sale/item cancellation, every supported filter (`customerName=Al*`, `_minTotalAmount`, ...), pagination/ordering and the documented error scenarios (400/404/409 + 401 for missing token).

### Run locally without Docker
```bash
docker compose up -d ambev.developerevaluation.database  # Postgres only
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi
```
The `appsettings.json` connection string already points to `localhost:5432`.

## Business rules

| Quantity of the same item | Discount | Allowed? |
|---|---|---|
| < 4 | 0% | yes |
| 4 to 9 | 10% | yes |
| 10 to 20 | 20% | yes |
| > 20 | — | **no** (`DomainException`) |

Cancellation:
- Cancelling the **sale** zeroes `TotalAmount` and flips `Status = Cancelled`. Items keep their history for auditing.
- Cancelling an **item** marks the item as cancelled, zeroes its `TotalAmount` and recomputes the sale total.

## Test scope and the `/.doc` folder

The `/.doc` folder ships docs from the original template that reference **Products**, **Carts**, **(extended) Users** and **Auth** APIs. Those references are **HTML-commented in the main README** (`<!-- ... -->`) and the use case for this assignment is explicit:

> *"You will write an API (complete CRUD) that handles **sales records**."*

So:
- **Implemented**: the Sales API (the actual assignment) following `DDD` + CQRS, with the business rules and the 4 optional events.
- **Out of scope** (but referenced in `/.doc`): Products / Carts routes and the extended User fields (`name.firstname/lastname`, `address`, `geolocation`, ...). The basic Auth/User feature shipped with the template was preserved.
- **General conventions applied to the Sales API** (from [`/.doc/general-api.md`](.doc/general-api.md)):
  - Paginated payload `{ data, totalItems, currentPage, totalPages }`
  - String filters with `*` wildcards and numeric/date ranges via `_min<Field>` / `_max<Field>`
  - Multi-field ordering via `_order=field1 desc, field2 asc`
  - Error envelope `{ type, error, detail }`

## Additional template documentation

- [Overview](/.doc/overview.md)
- [Tech Stack](/.doc/tech-stack.md)
- [Frameworks](/.doc/frameworks.md)
- [General API conventions](/.doc/general-api.md)
- [Project Structure](/.doc/project-structure.md)
