---
paths:
  - "**/appsettings*.json"
  - "docker-compose*.yml"
  - "docker-compose.dcproj"
  - "infra.compose.yml"
  - "**/Dockerfile"
  - "src/*/*.API/Extensions/**"
  - "src/*/*.API/Program.cs"
  - "src/ApiGateway/Program.cs"
  - "src/Payment/Payment.Processor/Program.cs"
  - "otel/**"
  - "loki/**"
  - "prometheus/**"
  - "grafana/**"
---

# Runtime configuration and observability

- **Configuration split:** each `appsettings.json` has only environment-agnostic, non-secret settings (Serilog, Jwt issuer/audience, token lifetimes, Payment options). Connection strings, `MessageBroker`, `Otlp`, `Jwt:SecretKey`, `Account:FrontendBaseUrl`, and `AdminUser` are in `appsettings.Development.json`, pointing at the host-published localhost ports, so a service started outside compose works against the compose infrastructure. Under compose, `docker-compose.override.yml` sets the container hostnames (`mssql-database`, `rabbitmq-broker`, `redis-caching`) as environment variables (`ConnectionStrings__EcommerceDB`, `MessageBroker__Host`, `ConnectionStrings__Redis`). A non-Development deployment must supply all of these itself. The integration-test `SystemFixture` overrides the broker, Redis, database, and Payment settings and takes the rest from `appsettings.Development.json`.
- `mssql-database` has a healthcheck and the services wait for it (`service_healthy`). Without it a service can start before SQL Server accepts logins and crash in `EnsureDeleted()`.
- The `logs/` folder in service containers is owned by root, so the non-root `app` user can't write the Serilog files there. Console output is not configured, so container logs are empty.
- Each service uses Serilog and exports OpenTelemetry logs, traces, and metrics over OTLP to `Otlp:*Endpoint`: `localhost:24318` from `appsettings.Development.json` for host runs, `otel-collector:4318` from `docker-compose.override.yml` under compose. The gateway exports too (no Serilog), and tracing includes MassTransit's source and meter, so one trace follows an order from the gateway through every consumer and outbox hop. The collector (`otel/otel-collector.yml`) sends logs to Loki, traces to Tempo, and metrics to Prometheus (scraped with `honor_labels`, so `job` is the service name); Grafana has all three provisioned. Each service and the gateway expose `/health`.
