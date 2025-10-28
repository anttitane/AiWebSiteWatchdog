# AiWebSiteWatchdog

Motivation
----------

Have you ever needed to check a website regularly to see if something relevant has been posted? I have. This project began when my hometown surprised residents by adding parking restriction signs on our street. The change was recorded in the city council minutes, but I hadn’t noticed them. Manually scanning the large volume of meeting minutes is time-consuming and needs to be done regularly. This application automates that work and notifies you when meeting minutes (or other monitored pages) contain items of interest.

Overview
--------

AiWebSiteWatchdog periodically scans configured websites for matches against user-defined interests and notifies users when relevant changes are detected. It uses Hangfire for scheduling, EF Core + SQLite for persistence, the Google Gemini generative API for content extraction/analysis, and Gmail API for notifications.

Security note
-------------

This service is intended to run on a private, trusted network (intranet, internal cloud VNet). Do not expose the application directly to the public internet without appropriate controls (authenticated reverse proxy, VPN, IP allowlist, WAF). See the Security & Secrets section for guidance.

Prerequisites
-------------

- .NET 9 SDK
- A Google Cloud project with Gmail & Generative Language (Gemini) APIs enabled
- (Optional) Docker for containerized runs

Key .NET conventions & technologies
----------------------------------

- Platform: .NET 9, minimal API (WebApplication) in `AiWebSiteWatchDog.API/Program.cs`
- DI: built-in Microsoft DI; register services with appropriate lifetimes
- Configuration: `appsettings.json` + environment variables (double-underscore mapping)
- Persistence: EF Core with SQLite; migrations included; design-time `AppDbContextFactory` uses env var for connection string
- Scheduling: Hangfire with SQLite storage; recurring jobs use `RecurringJob.AddOrUpdate`
- HTTP calls: typed `HttpClient` for `IGeminiApiClient`
- Logging: Serilog (configured via appsettings)
- Central package version management via Directory.Packages.props (PackageVersion entries).
- Clean architecture

Where to look in the codebase
----------------------------

- Startup & DI: `AiWebSiteWatchDog.API/Program.cs`
- Endpoints: `AiWebSiteWatchDog.API/Endpoints.cs`
- Background job runner: `AiWebSiteWatchDog.API/Jobs/WatchTaskJobRunner.cs`
- Gemini client: `AiWebSiteWatchDog.Infrastructure/Gemini/GeminiApiClient.cs`
- Email sending: `AiWebSiteWatchDog.Infrastructure/Email/EmailSender.cs`
- Google auth/token handling: `AiWebSiteWatchDog.Infrastructure/Auth/GoogleCredentialProvider.cs`
- Persistence: `AiWebSiteWatchDog.Infrastructure/Persistence/` and domain entities under `AiWebSiteWatchDog.Domain/Entities/`

Getting Google OAuth2 credentials
---------------------------------

This app uses the Gmail and Gemini API via Google Cloud and OAuth2 for secure email delivery and Gemini usage:

- **OAuth2 Security:** Your Google password is never seen or stored by the app. Instead, Google issues a secure, time-limited access token after you grant permission.
- **User Consent:** You must log in and explicitly approve access to your Gmail account. No email can be sent without your consent.
- **Google Cloud Controls:** All credentials are managed in Google Cloud Console, which provides strong security and access controls.
- **Token Storage:** Access tokens are stored securely and can be revoked by you at any time in your Google account settings.
- **API Scopes:** The app only requests the minimum required scope (`Send email on your behalf` and `Use Gemini models with your personal quota`), limiting what it can do with your account.

This means only authorized users can send email, credentials are never exposed, and Google’s infrastructure protects both authentication and email delivery.


To send email using the Gmail API, you need a Google OAuth2 client_secret.json file. Follow these steps:

1. Go to [Google Cloud Console](https://console.cloud.google.com/).
2. Create a new project (or select an existing one).
3. In the left menu, go to "APIs & Services" > "Library" and search for "Gmail API". Click "Enable". Do same for "Gemini API".
4. Go to "APIs & Services" > "Credentials".
5. Click "Create Credentials" > "OAuth client ID".
6. If prompted, configure the consent screen (fill in required fields).
7. Choose "Desktop app" (for local testing) or "Web application" (for server).
8. Enter a name and click "Create".
9. Download the `client_secret.json` file.
10. Open the file and copy its contents. Set it as an environment variable `GOOGLE_CLIENT_SECRET_JSON` (see Environment Variables section below).
11. On "Data access" tab add scopes for `.../auth/gmail.send` and `./auth/generative-language.peruserquota`
12. Save changes.

**Important:**
- If your OAuth consent screen is in testing mode, you must add your Google account as a test user in the "Test users" section of the consent screen (found in the audience tab).
- Alternatively, you can publish the app to make it available to all users in your organization or publicly.
- The app requests both Gmail send and Gemini (Generative Language) scopes using a single combined consent; if scopes change later you must re-consent (delete stored token row / token files).

Configuration (appsettings.json & environment variables)
------------------------------------------------------

The application reads configuration from `appsettings.json`. Environment variables override values from `appsettings.json`. Use double-underscore to set hierarchical keys as env vars (for example `ConnectionStrings__DefaultConnection` maps to `ConnectionStrings:DefaultConnection`).

Important configuration keys

| Key (appsettings path) | Example env var | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | SQLite/DB connection for EF Core |
| `ConnectionStrings:HangfireConnection` | `ConnectionStrings__HangfireConnection` | Hangfire storage connection |
| `Hangfire:WorkerCount` | `Hangfire__WorkerCount` | Number of Hangfire workers |
| `Google:UseDbTokenStore` | `Google__UseDbTokenStore` | Persist OAuth tokens to DB when true |
| `Google:TokensPath` | `Google__TokensPath` | Filesystem token cache base path (if not using DB) |

Secrets (do NOT commit)

- `GOOGLE_CLIENT_SECRET_JSON`: raw client_secret.json contents (required)
- `GOOGLE_TOKENS_ENCRYPTION_KEY`: Base64 AES key used to encrypt DB token rows (required if DB store is enabled)

Generating an encryption key
---------------------------

The app can generate a secure AES-256 key and exit:

```pwsh
dotnet run --project .\AiWebSiteWatchDog.API -- --generate-encryption-key
```

The command prints a Base64 string you should store in a secret manager and set as `GOOGLE_TOKENS_ENCRYPTION_KEY`.

Running locally (development)
-----------------------------

Set the required env vars and run:

```pwsh
$env:GOOGLE_CLIENT_SECRET_JSON = Get-Content -Raw .\client_secret.json
$env:ConnectionStrings__DefaultConnection = 'Data Source=AiWebSitewatchdog.db'
dotnet run --project .\AiWebSiteWatchDog.API
```

Docker (single container)
-------------------------

If you prefer a one-off run without Compose, you can run the container directly and inject configuration via environment variables:

```pwsh
# Replace ghcr.io/anttitane/aiwebsitewatchdog:latest with your image tag
docker run --rm -p 8080:8080 `
	-e ASPNETCORE_ENVIRONMENT=Development `
	-e ConnectionStrings__DefaultConnection='Data Source=/data/app.db' `
	-e ConnectionStrings__HangfireConnection='Data Source=/data/app.db;Cache=Shared;Mode=ReadWriteCreate;' `
	-e GOOGLE_CLIENT_SECRET_JSON="$($env:GOOGLE_CLIENT_SECRET_JSON)" `
	-v app_data:/data `
	-v app_logs:/app/logs `
	ghcr.io/anttitane/aiwebsitewatchdog:latest
```

Notes:
- The container listens on port 8080; adjust the left side of -p accordingly.
- We mount two named volumes: app_data for the SQLite file at /data/app.db and app_logs for Serilog file output at /app/logs.
- GOOGLE_CLIENT_SECRET_JSON should contain the content of your client_secret.json (do not bake it into the image).

Docker Compose (recommended)
----------------------------

This repo includes a `docker-compose.yml` that wires sensible defaults:
- Maps port 8080 → 8080
- Persists data and logs to named volumes (`app_data`, `app_logs`)
- Sets both EF Core and Hangfire to use the same SQLite DB at `/data/app.db`
- Enables console logging (visible via `docker compose logs`)

Basic usage:

```pwsh
# Build and start in the background
docker compose up -d --build

# Stream logs from the aiwebsitewatchdog container
docker compose logs -f aiwebsitewatchdog

# Stop and remove containers
docker compose down
```

Environment configuration:
- Override configuration via environment variables in `docker-compose.yml` (mapping syntax)
	- `ConnectionStrings__DefaultConnection: Data Source=/data/app.db`
	- `ConnectionStrings__HangfireConnection: Data Source=/data/app.db;Cache=Shared;Mode=ReadWriteCreate;`
	- `ASPNETCORE_ENVIRONMENT: Development` (so Swagger UI is available at `/swagger`)
- Provide secrets at runtime (example for PowerShell):

```pwsh
$env:GOOGLE_CLIENT_SECRET_JSON = Get-Content -Raw .\client_secret.json
# Optionally, set an encryption key if using DB token store
# $env:GOOGLE_TOKENS_ENCRYPTION_KEY = "<Base64-AES-256>"

# Then start compose (env vars are inherited by Docker on Windows)
docker compose up -d --build
```

Endpoints to try:
- Swagger UI: http://localhost:8080/swagger
- Health: http://localhost:8080/health
- Hangfire Dashboard: http://localhost:8080/hangfire

Inspecting logs and data volumes:

```pwsh
# Tail app logs (console)
docker compose logs -f aiwebsitewatchdog

# List log files persisted on the volume
docker compose exec aiwebsitewatchdog sh -lc "ls -la /app/logs"

# Copy logs to host
docker cp aiwebsitewatchdog:/app/logs .\logs-copy
```

Token storage behavior
----------------------

| Mode | Trigger | Location | Notes |
|---|---|---|---|
| Filesystem | `Google:UseDbTokenStore` = false | Hashed folder under OS profile/config path | Simpler for single-instance dev runs |
| Database (encrypted) | `Google:UseDbTokenStore` = true | `GoogleOAuthTokens` table | Requires `GOOGLE_TOKENS_ENCRYPTION_KEY` to decrypt; recommended for multi-instance deployments |

Logging
-------

Serilog is configured in `appsettings.json`. The file sink is set to daily roll and retention; adjust `Serilog` section in `appsettings.json` or via env vars to change behavior.
