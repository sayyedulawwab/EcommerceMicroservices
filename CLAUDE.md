# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Solution file is `EcommerceMicroservices.slnx` (.NET 10 SDK).

```bash
# Run the system. Infra (SQL Server, Redis, RabbitMQ) is a separate compose project that creates the shared
# external network `ecommerce-microservices-network`; start it first and leave it running.
docker compose -f infra.compose.yml up -d
# Services + gateway (or the VS "Docker Compose" profile). No depends_on on infra: it lives in another project.
docker compose -f docker-compose.yml -f docker-compose.override.yml up --build

# Build
dotnet build EcommerceMicroservices.slnx

# Tests (xUnit v3 + Moq, plain Assert). global.json opts dotnet test into Microsoft.Testing.Platform,
# so --project/--solution are required.
dotnet test --solution EcommerceMicroservices.slnx
dotnet test --project tests/Ordering/Ordering.Domain.UnitTests --filter-method "*ConfirmStock*"
# Integration tests need Docker (Testcontainers starts SQL Server, RabbitMQ, Redis)
dotnet test --project tests/IntegrationTests/EcommerceMicroservices.IntegrationTests

# EF Core migrations (Catalog, Identity, Ordering each have their own DbContext in *.Infrastructure)
dotnet ef migrations add <Name> -p src/Catalog/Catalog.Infrastructure -s src/Catalog/Catalog.API
```

`Directory.Build.Props` turns on `TreatWarningsAsErrors`, `AnalysisMode=All`, `EnforceCodeStyleInBuild`, and SonarAnalyzer for every project, test projects included. Analyzer and `.editorconfig` style violations fail the build.

## Known state

- **Keep MassTransit on 8.x.** MassTransit 9 is commercially licensed. Without `MT_LICENSE`, every service crashes at startup ("License must be specified"). `SharedKernel.csproj` pins `MassTransit.RabbitMQ` 8.5.11, and Catalog/Ordering pin `MassTransit.EntityFrameworkCore` 8.5.11 for the outbox.
- **Don't add FluentAssertions.** Version 8+ is commercially licensed; tests use xUnit's `Assert`.
- **NuGet audit warnings fail the local build.** `TreatWarningsAsErrors` promotes vulnerability advisories (NU1902/NU1903) to errors, so a newly published advisory can break restore with no code change. Bump the flagged package, or temporarily pass `-p:NuGetAudit=false`.
- **Docker builds don't apply `Directory.Build.Props`.** MSBuild looks for `Directory.Build.props` (lowercase), and Linux filesystems are case-sensitive. Analyzer warnings that break the Windows/Visual Studio build only show up as warnings inside `docker compose build`. Always verify with a local `dotnet build`.
- Each service Dockerfile must `COPY` every referenced `.csproj`, including `src/SharedKernel/SharedKernel.csproj`, before `dotnet restore`.
- `dotnet ef` scaffolds code that Sonar flags (e.g. S4581); `.editorconfig` turns such rules off under `**/Migrations/**`.

## Architecture

Five services plus a gateway. Each service has its own database or store. They share code only through `src/SharedKernel` and talk to each other only through RabbitMQ.

| Service | Layers | Storage | Notes |
|---|---|---|---|
| Identity | API/Application/Domain/Infrastructure | SQL Server `IdentityDB` | Accounts, auth cookies, refresh-token rotation, email/password-reset tokens |
| Catalog | API/Application/Domain/Infrastructure | SQL Server `CatalogDB` | Products, categories, stock reservations; the source of prices |
| Cart | API/Application/Domain/Infrastructure | Redis | No EF; cart lives in the cache |
| Ordering | API/Application/Domain/Infrastructure | SQL Server `OrderingDB` | Order lifecycle |
| Payment.Processor | single project | none | Simulated payment consumer (`Payment` options) |
| ApiGateway | single project | none | YARP, CORS, aggregated Swagger UI |

**Gateway** ([src/ApiGateway/appsettings.json](src/ApiGateway/appsettings.json)): `/api/{service}/v{version}/...` is rewritten to `/api/v{version}/...` on the service's cluster destination, and `/swagger/{service}/...` proxies each service's Swagger document (the service rewrites its paths to gateway paths when it sees `X-Forwarded-Prefix`). Payment has no route. `appsettings.json` holds only the routes. The clusters (destination addresses) and `Cors:AllowedOrigins` live in `appsettings.Development.json` (localhost host ports), and `docker-compose.override.yml` overrides the addresses with `http://{service}-api:8080/` through `ReverseProxy__Clusters__...` environment variables. CORS allows credentialed requests only from `Cors:AllowedOrigins`.

**Auth:**
- Identity puts the access token and the refresh token in HttpOnly, Secure, `SameSite=None` cookies (`access_token`, `refresh_token`; names in `SharedKernel/Auth/AuthCookies.cs`). Login, refresh and change-password return 204 with cookies, never tokens in a body. The frontend reads the user from `GET /users/me`. Cookie domain, SameSite and refresh-cookie path are in the `AuthCookies` section.
- Every service validates JWTs with the same `Jwt` section. `*.Infrastructure/Auth/JwtBearerOptionsSetup` reads the token from the cookie first and falls back to `Authorization: Bearer` (Swagger, tests).
- Admin endpoints use `[Authorize(Roles = Roles.Admin)]`. The role claim is added for `User.IsAdmin`. The first admin is created or promoted at startup from the `AdminUser` section (`Identity.Infrastructure/Data/AdminSeeder.cs`).
- **CSRF:** because cookies are `SameSite=None`, `*.API/Middleware/CsrfProtectionMiddleware` rejects (403) cookie-authenticated POST/PUT/PATCH/DELETE requests that have neither `Content-Type: application/json` nor `X-Requested-With`. Either one forces a CORS preflight. The frontend must send one of them on every write, including body-less POSTs like `/auth/refresh`.
- Refresh tokens and emailed tokens are stored as SHA-256 hashes. Refresh tokens rotate within a `FamilyId` (one per login). Replaying a token revoked more than 30 s ago revokes the whole family. Logout revokes the family. Password change and reset revoke every family. `TokenCleanupService` deletes stale rows.
- Emails (confirmation, password reset) go through `IEmailSender`. `LoggingEmailSender` only logs them, so swap the registration for a real sender. Links point to `Account:FrontendBaseUrl`.

### Clean architecture / CQRS (no MediatR)

- `SharedKernel/Messaging` defines `ICommand`, `ICommand<T>`, `IQuery<T>`, and their handlers. Handlers return `Result`/`Result<T>` (`SharedKernel/Domain/Result.cs`) and don't throw for expected failures.
- Each `*.Application/DependencyInjection.cs` registers handlers by Scrutor assembly scan, then wraps them with decorators: `ValidationDecorator` (runs FluentValidation validators and returns `ValidationError`) and `LoggingDecorator`. A new handler is picked up automatically. A new validator only needs to exist in the assembly.
- Controllers inject the handler directly into the action method, e.g. `ICommandHandler<AddProductCommand, long> handler`, and map failures with `result.Error.ToActionResult()`. `Error.Code` (a `System.Net.HttpStatusCode`) is the HTTP status. Routes are versioned: `[ApiVersion(1)]`, `api/v{v:apiVersion}/...`.
- Application features are organized as vertical slices: `Application/<Aggregate>/<UseCase>/` holds the Command/Query, Handler, and Validator.
- Domain entities derive from `Entity<TId>` and raise domain events. `ApplicationDbContext.SaveChangesAsync` dispatches them through `DomainEventsDispatcher` after the save succeeds, each handler in its own DI scope. Identity uses this to mail token links (`UserTokenIssuedDomainEvent`). The DbContext also implements `IUnitOfWork`, including `ExecuteInTransactionAsync`, and turns `DbUpdateConcurrencyException` into `ConcurrencyException`, which the API maps to 409. `Product` and `Order` have `RowVersion` concurrency tokens.

### Integration events (MassTransit + RabbitMQ)

Event contracts live in `SharedKernel/Events`. Consumers implement `IIntegrationEventHandler<T>` (a MassTransit `IConsumer<T>`) and are registered explicitly with `AddConsumer<>` in each service's `Program.cs`. You must add any new consumer there. Queue names are prefixed with the service name (`KebabCaseEndpointNameFormatter("catalog", false)`), so each service gets its own copy of an event. Consumers log a failed `Result` and ack the message. Exceptions are retried (exponential, 5 times) and then go to the `_error` queue.

Order flow (a choreographed saga). The client sends only product ids and quantities:

1. Ordering `PlaceOrder` → `OrderPlacedIntegrationEvent` (status `Placed`, no prices yet)
2. Catalog reserves stock (decrements `Quantity`, writes `StockReservations`) and answers `OrderItemsStockConfirmed` with Catalog's names and prices, or `OrderItemsStockRejected` (unknown product, not enough stock, mixed currencies)
3. Ordering prices the order and computes the total → `StockConfirmed`, publishes `OrderStatusChangedToStockConfirmed` (with amount); on rejection → `Cancelled` + `OrderCancelled`
4. Payment.Processor charges the amount → `OrderPaymentSucceeded` / `OrderPaymentFailed`
5. Ordering → `Paid` + `OrderStatusChangedToPaid` (Catalog deletes the reservations, Cart clears the cart); on failure → `Cancelled` + `OrderCancelled` (Catalog releases the reserved stock)
6. Admin moves `Paid → Shipped → Delivered`. Customers may cancel while `Placed`/`StockConfirmed`.

Every cancellation publishes `OrderCancelled`, and releasing stock is idempotent. If stock gets confirmed for an already-cancelled order, Ordering publishes `OrderCancelled` again. A payment that succeeds for an order the customer cancelled meanwhile is only logged. There are no refunds.

**Outbox:** Catalog and Ordering use MassTransit's EF Core outbox and inbox (tables in their DBs). Publish before `SaveChangesAsync` so the event is stored in the same transaction. Inside consumers this happens automatically. `PlaceOrder` needs the generated order id, so it saves, publishes and saves again inside `ExecuteInTransactionAsync`. Cart and Payment have no database: Cart's handler is idempotent, and Payment uses the in-memory outbox.

### Runtime behavior to be aware of

- **In Development, Catalog, Identity, and Ordering call `Database.EnsureDeleted()` and then `Migrate()` on startup** (`*.API/Extensions/MigrationExtensions.cs`). Every restart wipes their databases. Catalog then reseeds from `Infrastructure/Data/DataSeeder.cs`. Identity then seeds the admin (`admin@ecommerce.local` / `Admin123!` by default).
- **Configuration split:** each `appsettings.json` has only environment-agnostic, non-secret settings (Serilog, Jwt issuer/audience, token lifetimes, Payment options). Connection strings, `MessageBroker`, `Otlp`, `Jwt:SecretKey`, `Account:FrontendBaseUrl`, and `AdminUser` are in `appsettings.Development.json`, pointing at the host-published localhost ports, so a service started outside compose works against the compose infrastructure. Under compose, `docker-compose.override.yml` sets the container hostnames (`mssql-database`, `rabbitmq-broker`, `redis-caching`) as environment variables (`ConnectionStrings__EcommerceDB`, `MessageBroker__Host`, `ConnectionStrings__Redis`). A non-Development deployment must supply all of these itself. The integration-test `SystemFixture` overrides the broker, Redis, database, and Payment settings and takes the rest from `appsettings.Development.json`.
- `mssql-database` has a healthcheck and the services wait for it (`service_healthy`). Without it a service can start before SQL Server accepts logins and crash in `EnsureDeleted()`.
- Cart rebuilds cached carts with Newtonsoft.Json (`CacheService`), so cached types such as `Currency` need a (private) parameterless constructor.
- The `logs/` folder in service containers is owned by root, so the non-root `app` user can't write the Serilog files there. Console output is not configured, so container logs are empty.
- Each service uses Serilog and exports OpenTelemetry logs, traces, and metrics over OTLP to `Otlp:*Endpoint`: `localhost:24318` from `appsettings.Development.json` for host runs, `otel-collector:4318` from `docker-compose.override.yml` under compose. The gateway exports too (no Serilog), and tracing includes MassTransit's source and meter, so one trace follows an order from the gateway through every consumer and outbox hop. The collector (`otel/otel-collector.yml`) sends logs to Loki, traces to Tempo, and metrics to Prometheus (scraped with `honor_labels`, so `job` is the service name); Grafana has all three provisioned. Each service and the gateway expose `/health`.
- Host ports: gateway 7000, Catalog 5000, Cart 5002, Ordering 5004, Identity 5006, Payment 5008, RabbitMQ UI 15672 / AMQP 15673, Redis 16379, SQL Server 1433 (`sa`/`Passw0rd`), OTLP 24318, Grafana 13000 (`admin`/`admin`), Prometheus 19090, Loki 13100, Tempo 13200. The observability ports and container names (`microservices-*`) are offset so this stack can run next to another project's. Redis isn't published on 6379 because Windows (Hyper-V/WinNAT) often reserves the 63xx range. Run `netsh interface ipv4 show excludedportrange protocol=tcp` if a port fails to bind.

### Tests

- `tests/<Service>/<Service>.Domain.UnitTests` and `.Application.UnitTests`: Application projects expose internals to their test project via `InternalsVisibleTo`.
- `tests/IntegrationTests`: `SystemFixture` starts the containers and hosts every service in-process with `WebApplicationFactory` (one shared collection, so the classes run one after another). The API projects are referenced with `extern alias`es because each has a global `Program`. `TestJwt` signs bearer tokens with the shared `Jwt` settings.
