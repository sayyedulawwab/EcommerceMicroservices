# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Topic rules in `.claude/rules/` load when matching files are opened: `clean-architecture.md` (C# under `src/`: CQRS, data access, IDs, domain rules, events), `messaging.md` (integration events, order saga, outbox), `gateway-and-auth.md` (YARP gateway, cookies, JWT, CSRF, refresh tokens), `runtime-and-observability.md` (configuration split, compose, Serilog and OpenTelemetry), `build-and-packages.md` (package audit, Dockerfiles, migrations), `testing.md` (tests and CI).

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

# Tests (xUnit v3, plain Assert). global.json opts dotnet test into Microsoft.Testing.Platform,
# so --project/--solution are required.
dotnet test --solution EcommerceMicroservices.slnx
dotnet test --project tests/Ordering/Ordering.Domain.UnitTests --filter-method "*ConfirmStock*"
# Architecture tests (layer and service dependencies, ArchUnitNET)
dotnet test --project tests/ArchitectureTests/EcommerceMicroservices.ArchitectureTests
# Integration tests need Docker (Testcontainers starts SQL Server, RabbitMQ, Redis)
dotnet test --project tests/IntegrationTests/EcommerceMicroservices.IntegrationTests

# EF Core migrations (Catalog, Identity, Ordering each have their own DbContext in *.Infrastructure)
dotnet ef migrations add <Name> -p src/Catalog/Catalog.Infrastructure -s src/Catalog/Catalog.API
```

`Directory.Build.props` sets the target framework, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`, `AnalysisMode=All` and `EnforceCodeStyleInBuild` for every project, test projects included. Analyzer and `.editorconfig` style violations fail the build. `Directory.Packages.props` holds every package version (central package management with transitive pinning) and adds SonarAnalyzer to every project as a `GlobalPackageReference`. Project files list `PackageReference` items without versions; a `Version` there fails restore with NU1008.

## Known state

- **Keep MassTransit on 8.x.** MassTransit 9 is commercially licensed. Without `MT_LICENSE`, every service crashes at startup ("License must be specified"). `SharedKernel.csproj` pins `MassTransit.RabbitMQ` 8.5.11, and Catalog/Ordering pin `MassTransit.EntityFrameworkCore` 8.5.11 for the outbox.
- **Don't add FluentAssertions.** Version 8+ is commercially licensed; tests use xUnit's `Assert`.

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

### Runtime behavior to be aware of

- **In Development, Catalog, Identity, and Ordering call `Database.EnsureDeleted()` and then `Migrate()` on startup** (`*.API/Extensions/DevelopmentDatabaseExtensions.cs`). Every restart wipes their databases. Catalog then reseeds from `Infrastructure/Data/DataSeeder.cs`. Identity then seeds the admin (`admin@ecommerce.local` / `Admin123!` by default).
- Host ports: gateway 7000, Catalog 5000, Cart 5002, Ordering 5004, Identity 5006, Payment 5008, RabbitMQ UI 15672 / AMQP 15673, Redis 16379, SQL Server 1433 (`sa`/`Passw0rd`), OTLP 24318, Grafana 13000 (`admin`/`admin`), Prometheus 19090, Loki 13100, Tempo 13200. The observability ports and container names (`microservices-*`) are offset so this stack can run next to another project's. Redis isn't published on 6379 because Windows (Hyper-V/WinNAT) often reserves the 63xx range. Run `netsh interface ipv4 show excludedportrange protocol=tcp` if a port fails to bind.
