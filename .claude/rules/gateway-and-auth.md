---
paths:
  - "src/ApiGateway/**"
  - "src/Identity/**"
  - "src/SharedKernel/Auth/**"
  - "src/*/*.Infrastructure/Auth/**"
  - "src/*/*.API/Middleware/**"
  - "tests/Identity/**"
  - "tests/IntegrationTests/**/Identity/**"
---

# Gateway and auth

**Gateway** ([src/ApiGateway/appsettings.json](../../src/ApiGateway/appsettings.json)): `/api/{service}/v{version}/...` is rewritten to `/api/v{version}/...` on the service's cluster destination, and `/swagger/{service}/...` proxies each service's Swagger document (the service rewrites its paths to gateway paths when it sees `X-Forwarded-Prefix`). Payment has no route. `appsettings.json` holds only the routes. The clusters (destination addresses) and `Cors:AllowedOrigins` live in `appsettings.Development.json` (localhost host ports), and `docker-compose.override.yml` overrides the addresses with `http://{service}-api:8080/` through `ReverseProxy__Clusters__...` environment variables. CORS allows credentialed requests only from `Cors:AllowedOrigins`.

**Auth:**
- Identity puts the access token and the refresh token in HttpOnly, Secure, `SameSite=None` cookies (`access_token`, `refresh_token`; names in `SharedKernel/Auth/AuthCookies.cs`). Login, refresh and change-password return 204 with cookies, never tokens in a body. The frontend reads the user from `GET /users/me`. Cookie domain, SameSite and refresh-cookie path are in the `AuthCookies` section.
- Every service validates JWTs with the same `Jwt` section. `*.Infrastructure/Auth/JwtBearerOptionsSetup` reads the token from the cookie first and falls back to `Authorization: Bearer` (Swagger, tests).
- Admin endpoints use `[Authorize(Roles = Roles.Admin)]`. The role claim is added for `User.IsAdmin`. The first admin is created or promoted at startup from the `AdminUser` section (`Identity.Infrastructure/Data/AdminSeeder.cs`).
- **CSRF:** because cookies are `SameSite=None`, `*.API/Middleware/CsrfProtectionMiddleware` rejects (403) cookie-authenticated POST/PUT/PATCH/DELETE requests that have neither `Content-Type: application/json` nor `X-Requested-With`. Either one forces a CORS preflight. The frontend must send one of them on every write, including body-less POSTs like `/auth/refresh`.
- Refresh tokens and emailed tokens are stored as SHA-256 hashes. Refresh tokens rotate within a `FamilyId` (one per login). `RefreshToken` has a `RowVersion` concurrency token, so when two requests rotate the same token at once the loser's save throws `ConcurrencyException`, and `RefreshTokensCommandHandler` turns that into the normal invalid-token 401 (not the 409 the API maps it to elsewhere). Replaying a token revoked more than 30 s ago revokes the whole family. Logout revokes the family. Password change and reset revoke every family. `TokenCleanupService` deletes stale rows.
- Emails (confirmation, password reset) go through `IEmailSender`. `LoggingEmailSender` only logs them, so swap the registration for a real sender. Links point to `Account:FrontendBaseUrl`.
