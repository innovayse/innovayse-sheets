# Innovayse Sheets

A Google-Sheets-style collaborative spreadsheet service: an ASP.NET Core 9 backend (formula evaluation, sharing/access control, SignalR-based real-time presence and live cell updates) with a Nuxt/Vue 3 client.

## Architecture

- `backend/` — ASP.NET Core 9 Web API (`Innovayse.Sheets.API`), EF Core + PostgreSQL, SignalR hub for realtime collaboration, a small in-house formula evaluation library (`Innovayse.Sheets.Formulas`).
- `client/` — Nuxt 3 / Vue 3 frontend: grid with headers/selection/formula bar/toolbar, sheet tabs, undo/redo, sharing UI.

## Running locally

Backend (requires PostgreSQL — see `backend/docker-compose.yml` for a local dev instance):

```bash
cd backend
docker compose up -d          # starts a local Postgres
cp src/Innovayse.Sheets.API/appsettings.Development.json.example src/Innovayse.Sheets.API/appsettings.Development.json
# edit appsettings.Development.json — see "Authentication" below for the required SSO values
dotnet run --project src/Innovayse.Sheets.API
```

Client:

```bash
cd client
npm install
npm run dev
```

## Authentication

This backend does **not** implement its own user accounts or login. It validates JWT Bearer access tokens issued by an external OIDC-compatible identity provider, configured via:

```json
{
  "Sso": {
    "Authority": "https://your-sso-issuer.example.com",
    "Audience": "innovayse-sheets"
  }
}
```

Any standards-compliant OIDC provider that issues access tokens with an `aud` claim matching the configured `Sso:Audience` value will work for basic authentication.

**Share-by-email additionally requires** a service-to-service user-lookup endpoint on that same identity provider, at:

```
GET {Sso:Authority}/api/service/users/lookup?email={email}
```

protected by a shared static key sent as an `X-Service-Key` header (configured via `ServiceAuth:ApiKey` on both sides), returning `200 { "userId": "<guid>" }` on a match or `404` otherwise. This shape is specific to how this project's original deployment (Innovayse's own SSO) resolves an email to a user id — if you're running this against a different identity provider, you'll need to either implement a compatible endpoint there, or replace `Users/HttpSsoUserDirectory.cs`'s implementation of `ISsoUserDirectory` with one that talks to your provider's actual user-lookup API.

## License

MIT — see `LICENSE`.
