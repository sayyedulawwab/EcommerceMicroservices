---
paths:
  - "tests/**"
  - ".github/workflows/**"
---

# Tests

- `tests/<Service>/<Service>.Domain.UnitTests`: domain rules, no mocks. `Identity.Infrastructure.UnitTests` covers the password hasher and token service. `Payment.Processor.UnitTests` mocks `IPaymentService`.
- Handlers are tested through the API or the bus in `tests/IntegrationTests` (`<Service>/` folders), against the real containers. Don't mock the DbContext or DbSets, and don't use SQLite or the InMemory provider.
- `tests/ArchitectureTests`: ArchUnitNET rules, checked for each service: Domain depends on no other layer, Application not on Infrastructure or API, Infrastructure not on API, controllers not on Infrastructure. No service depends on another service's assemblies, and SharedKernel depends on no service.
- CI (`.github/workflows/ci.yml`) restores, builds (Release) and tests `EcommerceMicroservices.slnx` on every push and on pull requests to `main`.
- `tests/IntegrationTests`: `SystemFixture` starts the containers and hosts every service in-process with `WebApplicationFactory` (one shared collection, so the classes run one after another). The API projects are referenced with `extern alias`es because each has a global `Program`. `TestJwt` signs bearer tokens with the shared `Jwt` settings.
