# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Solution file is `EcommerceMicroservices.slnx` (.NET 10 SDK).

```bash
# Run the whole system (SQL Server, Redis, RabbitMQ, all services, gateway)
docker-compose up --build

# Build
dotnet build EcommerceMicroservices.slnx

# Tests (xUnit + FluentAssertions + Moq)
dotnet test EcommerceMicroservices.slnx
dotnet test tests/Catalog/Catalog.Domain.UnitTests --filter "FullyQualifiedName~<TestClass>.<TestMethod>"

# EF Core migrations (Catalog, Identity, Ordering each have their own DbContext in *.Infrastructure)
dotnet ef migrations add <Name> -p src/Catalog/Catalog.Infrastructure -s src/Catalog/Catalog.API
```

`Directory.Build.Props` turns on `TreatWarningsAsErrors`, `AnalysisMode=All`, `EnforceCodeStyleInBuild`, and SonarAnalyzer for every project. Analyzer and `.editorconfig` style violations fail the build.

## Known state

- **Keep MassTransit on 8.x.** MassTransit 9 is commercially licensed. Without `MT_LICENSE`, every service crashes at startup ("License must be specified"). `SharedKernel.csproj` pins `MassTransit.RabbitMQ` 8.5.11.
- **NuGet audit warnings fail the local build.** `TreatWarningsAsErrors` promotes vulnerability advisories (NU1902/NU1903) to errors, so a newly published advisory can break restore with no code change. Bump the flagged package, or temporarily pass `-p:NuGetAudit=false`.
- **Docker builds don't apply `Directory.Build.Props`.** MSBuild looks for `Directory.Build.props` (lowercase), and Linux filesystems are case-sensitive. Analyzer warnings that break the Windows/Visual Studio build only show up as warnings inside `docker compose build`. Always verify with a local `dotnet build`.
- **Test projects have no tests yet.** Only `tests/Catalog/*` exist, and they contain only the `.csproj`. `dotnet test` discovers nothing.
- **Bug: Catalog and Cart compete for `OrderStatusChangedToPaid`.** Both services have a consumer class named `OrderStatusChangedToPaidIntegrationEventHandler`. `SetKebabCaseEndpointNameFormatter()` gives both the same queue, so each message reaches only one of them: either stock is decremented or the cart is cleared, never both.
- Each service Dockerfile must `COPY` every referenced `.csproj`, including `src/SharedKernel/SharedKernel.csproj`, before `dotnet restore`.

## Architecture

Five services plus a gateway. Each service has its own database or store. They share code only through `src/SharedKernel` and talk to each other only through RabbitMQ.

| Service | Layers | Storage | Notes |
|---|---|---|---|
| Identity | API/Application/Domain/Infrastructure | SQL Server `IdentityDB` | Issues JWTs and refresh tokens |
| Catalog | API/Application/Domain/Infrastructure | SQL Server `CatalogDB` | Products, categories, stock |
| Cart | API/Application/Domain/Infrastructure | Redis | No EF; cart lives in the cache |
| Ordering | API/Application/Domain/Infrastructure | SQL Server `OrderingDB` | Drives the order workflow |
| Payment.Processor | single project | none | Simulated payment consumer |
| ApiGateway | single project | none | YARP |

**Gateway routing** ([src/ApiGateway/appsettings.json](src/ApiGateway/appsettings.json)): `/api/{service}/v{version}/...` is rewritten to `/api/v{version}/...` on `http://{service}-api:8080`. Payment has no route. Cluster addresses use docker-compose service names, so the gateway only works inside compose.

**Auth:** every service validates JWTs with the same `Jwt` section (Issuer/Audience/SecretKey) in its `appsettings.json`. Each `*.Infrastructure/Auth` folder configures it through `JwtOptionsSetup` and `JwtBearerOptionsSetup`.

### Clean architecture / CQRS (no MediatR)

- `SharedKernel/Messaging` defines `ICommand`, `ICommand<T>`, `IQuery<T>`, and their handlers. Handlers return `Result`/`Result<T>` (`SharedKernel/Domain/Result.cs`) and don't throw for expected failures.
- Each `*.Application/DependencyInjection.cs` registers handlers by Scrutor assembly scan, then wraps them with decorators: `ValidationDecorator` (runs FluentValidation validators and returns `ValidationError`) and `LoggingDecorator`. A new handler is picked up automatically. A new validator only needs to exist in the assembly.
- Controllers inject the handler directly into the action method, e.g. `ICommandHandler<AddProductCommand, long> handler`, and map failures with `result.Error.ToActionResult()`. `Error.Code` (`HttpResponseStatusCodes`) decides the HTTP status. Routes are versioned: `[ApiVersion(1)]`, `api/v{v:apiVersion}/...`.
- Application features are organized as vertical slices: `Application/<Aggregate>/<UseCase>/` holds the Command/Query, Handler, and Validator.
- Domain entities derive from `Entity<TId>` and raise domain events. `ApplicationDbContext.SaveChangesAsync` dispatches them through `DomainEventsDispatcher` after the save succeeds. It also implements `IUnitOfWork` and turns `DbUpdateConcurrencyException` into `ConcurrencyException`.

### Integration events (MassTransit + RabbitMQ)

Event contracts live in `SharedKernel/Events`. Consumers implement `IIntegrationEventHandler<T>` (a MassTransit `IConsumer<T>`) and are registered explicitly with `AddConsumer<>` in each service's `Program.cs`. You must add any new consumer there. Order flow (a choreographed saga):

1. Ordering `PlaceOrder` → `OrderPlacedIntegrationEvent`
2. Catalog checks stock → `OrderItemsStockConfirmed` / `OrderItemsStockRejected`
3. Ordering updates status → `OrderStatusChangedToStockConfirmed`
4. Payment.Processor → `OrderPaymentSucceeded` / `OrderPaymentFailed`
5. Ordering marks paid → `OrderStatusChangedToPaid`, consumed by Catalog (decrements stock) and Cart (clears the cart)

Events are published straight to the bus after the DB save. There is no outbox.

### Runtime behavior to be aware of

- **In Development, Catalog, Identity, and Ordering call `Database.EnsureDeleted()` and then `Migrate()` on startup** (`*.API/Extensions/MigrationExtensions.cs`). Every restart wipes their databases. Catalog then reseeds from `Infrastructure/Data/DataSeeder.cs`.
- Connection strings, the RabbitMQ host, and the Redis host in `appsettings.json` use docker-compose hostnames (`mssql-database`, `rabbitmq-broker`, `redis-caching`). Running a service outside compose needs overrides.
- Each service uses Serilog and exports OpenTelemetry logs, traces, and metrics over OTLP to `Otlp:*Endpoint` (default `localhost:14318`).
- Host ports: gateway 7000, Catalog 5000, Cart 5002, Ordering 5004, Identity 5006, Payment 5008, RabbitMQ UI 15672 / AMQP 15673, Redis 16379, SQL Server 1433 (`sa`/`Passw0rd`). Redis isn't published on 6379 because Windows (Hyper-V/WinNAT) often reserves the 63xx range. Run `netsh interface ipv4 show excludedportrange protocol=tcp` if a port fails to bind.
