# AiWebSiteWatchdog

### About
Have you ever needed to check a website regularly to see if something relevant has been posted? I have. This project began when my hometown surprised residents by adding parking restriction signs on our street. The change was recorded in the city council minutes, but I hadn’t noticed them. Manually scanning the large volume of meeting minutes is time-consuming and needs to be done regularly. This application automates that work and notifies you when meeting minutes (or other monitored pages) contain items of interest.

### Overview
AiWebSiteWatchdog periodically scans configured websites and alerts you when content matches your interests. You describe what you care about in plain English (a natural‑language prompt), and the built‑in AI (Gemini) reads the page and decides if it’s relevant—no regexes or anything complicated required. Under the hood it uses Hangfire for scheduling, EF Core + SQLite for persistence, the Google Gemini generative API for content extraction/analysis, and Gmail API for notifications.

The project also includes a lightweight single‑page application (SPA) built with React + TypeScript + Vite. In development, the UI runs on the Vite dev server (default http://localhost:5173) and proxies API calls to the backend (default http://localhost:5050). In production, the UI is prebuilt and served as static files by the ASP.NET Core app (Kestrel) from `wwwroot`.

### Key conventions & technologies
#### Backend
- Platform: .NET 9, minimal API (WebApplication) in `AiWebSiteWatchDog.API/Program.cs`
- DI: built-in Microsoft DI; register services with appropriate lifetimes
- Configuration: `appsettings.json` + environment variables (double-underscore mapping)
- Persistence: EF Core with SQLite; migrations included; design-time `AppDbContextFactory` uses env var for connection string
- Scheduling: Hangfire with SQLite storage; recurring jobs use `RecurringJob.AddOrUpdate`
- HTTP calls: typed `HttpClient` for `IGeminiApiClient`
- Logging: Serilog (configured via appsettings)
- Central package version management via Directory.Packages.props (PackageVersion entries).
- Clean architecture

#### Frontend
- Stack: React 18 + TypeScript + Vite in `AiWebSiteWatchDog.API/ClientApp`
- HTTP client: Axios with a shared `api` instance and small `services/` layer (`settings`, `tasks`, `notifications`)
- Dev server: Vite on port 5173 with proxy to the API on 5050 (configurable); optional `VITE_BACKEND_URL` for absolute base URL
- Build & hosting: `vite build` output is copied to `wwwroot` and served by Kestrel; Docker multi‑stage build includes the frontend
- DX: simple components with modals for Settings/Tasks/Notifications; VS Code tasks/launch run API + Vite together for F5 debugging

## Running AiWebSiteWatchdog
### 1. Getting Google OAuth2 credentials

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
7. Choose "Web application".
8. For "Authorised redirect URIs" add your `/auth/callback`endpoint URL (eg. http://localhost:8080/auth/callback)
9. Enter a name and click "Create".
10. Download the `client_secret.json` file. It contains your OAuth client_id and client_secret plus metadata (redirect URIs, etc.). The client_id identifies the app for Google. The client_secret is used during the authorization code exchange and token refresh. Think of it as the app’s credentials, distinct from the user’s credentials. OAuth is “delegated” access: the user grants permission, but the app must present its own identity when exchanging/refreshing tokens.
11. Open the file and copy its contents. Set it as an environment variable `GOOGLE_CLIENT_SECRET_JSON` (see instructions below).
12. On "Data access" tab add scopes for `.../auth/gmail.send` and `./auth/generative-language.peruserquota`
13. Save changes.

**Important:**
- If your OAuth consent screen is in testing mode, you must add your Google account as a test user in the "Test users" section of the consent screen (found in the audience tab).
	- If your OAuth consent screen is not “In production”, tokens time out on or shortly after day 7 and you need to re-authenticate. 
- Alternatively, you can publish the app to make it available to all users in your organization or publicly.
	- This app uses sensitive scope (Gmail). This means that you need to send your Google OAuth app for verification. 
	- If you have a Google Workspace domain: set the consent screen User Type to Internal. Internal apps don’t expire tokens and never show the unverified warning to domain users (you).
- The app requests both Gmail send and Gemini (Generative Language) scopes using a single combined consent; if scopes change later you must re-consent (delete stored token row / token files).

### 2. Running the Application
**Security note:
This service is intended to run on a private, trusted network (intranet, internal cloud VNet). Do not expose the application directly to the public internet without appropriate controls (authenticated reverse proxy, VPN, IP allowlist, WAF). See the Security & Secrets section for guidance.**

#### Docker Setup
The quickest way to get started is to pull and run the prebuilt image from GitHub Container Registry (GHCR):

```pwsh
docker pull ghcr.io/anttitane/aiwebsitewatchdog:latest
```

Then run it with your configuration and two named volumes (for the SQLite DB and logs):

Windows (PowerShell):

```pwsh
# Load client secret JSON content into an env var
$env:GOOGLE_CLIENT_SECRET_JSON = Get-Content -Raw .\client_secret.json

# Generate an encryption key (one-time) using the image
docker run --rm ghcr.io/anttitane/aiwebsitewatchdog:latest --generate-encryption-key

# Save the printed Base64 value
$env:GOOGLE_TOKENS_ENCRYPTION_KEY = "<Base64-AES-256>"

# Run the container (example)
docker run -d --name aiwebsitewatchdog -p 8080:8080 `
	-e ASPNETCORE_ENVIRONMENT=Development `
	-e ConnectionStrings__DefaultConnection='Data Source=/data/app.db' `
	-e ConnectionStrings__HangfireConnection='Data Source=/data/app.db;Cache=Shared;Mode=ReadWriteCreate;' `
	-e USE_DB_TOKEN_STORE=true `
	-e GOOGLE_TOKENS_ENCRYPTION_KEY="$env:GOOGLE_TOKENS_ENCRYPTION_KEY" `
	-e GOOGLE_CLIENT_SECRET_JSON="$env:GOOGLE_CLIENT_SECRET_JSON" `
	-v app_data:/data `
	-v app_logs:/app/logs `
	ghcr.io/anttitane/aiwebsitewatchdog:latest
```

Ubuntu Linux (bash):

```bash
# Copy client secret JSON file to folder of your choice

# Generate an encryption key (one-time) using the image
docker run --rm ghcr.io/anttitane/aiwebsitewatchdog:latest --generate-encryption-key

# Save the printed Base64 value
export GOOGLE_TOKENS_ENCRYPTION_KEY="<Base64-AES-256>"

# Run the container (example)
docker run -d --name aiwebsitewatchdog -p 8080:8080 \
	-e ASPNETCORE_ENVIRONMENT=Development \
	-e TZ="Europe/Helsinki" \
	-e ConnectionStrings__DefaultConnection='Data Source=/data/app.db' \
	-e ConnectionStrings__HangfireConnection='Data Source=/data/app.db;Cache=Shared;Mode=ReadWriteCreate;' \
	-e USE_DB_TOKEN_STORE=true \
	-e GOOGLE_TOKENS_ENCRYPTION_KEY="$GOOGLE_TOKENS_ENCRYPTION_KEY" \
	-e GOOGLE_CLIENT_SECRET_JSON_FILE="/home/username/aiwebsitewatchdog/client_secret.json" \
	-v /etc/localtime:/etc/localtime:ro \
	-v /etc/timezone:/etc/timezone:ro \
	-v app_data:/data \
	-v app_logs:/app/logs \
	ghcr.io/anttitane/aiwebsitewatchdog:latest

# Ensure Docker starts at boot so the container restarts on reboot
sudo systemctl enable docker
```

Tip (Compose/Portainer): To ensure the container uses your host timezone (so schedules run at local time), add the TZ environment variable and mount the host timezone files in your stack YAML:

```yaml
services:
	aiwebsitewatchdog:
		environment:
			TZ: Europe/Helsinki
		volumes:
			- /etc/localtime:/etc/localtime:ro
			- /etc/timezone:/etc/timezone:ro
```

### 3. Configuring

Once running:
- User interface: http://localhost:8080
- Swagger UI: http://localhost:8080/swagger
- Health: http://localhost:8080/health
- Hangfire Dashboard: http://localhost:8080/hangfire

Configuring:
- Add your settings
- Authorize Google. 
	- **Note:** Do this on your server using a web browser (this must be done on a server that has a graphical desktop) and give consent to scopes (Gmail send + Gemini). You need to do this only once. Note that it's not possible to complete the consent flow from another PC (than your server) even on your local network. 
- Create tasks

## Development instructions

### Prerequisites
- .NET 9 SDK
- A Google Cloud project with Gmail & Generative Language (Gemini) APIs enabled
- (Optional) Docker for containerized runs

### Where to look in the codebase
- Startup & DI: `AiWebSiteWatchDog.API/Program.cs`
- Endpoints: `AiWebSiteWatchDog.API/Endpoints.cs`
- Background job runner: `AiWebSiteWatchDog.API/Jobs/WatchTaskJobRunner.cs`
- Gemini client: `AiWebSiteWatchDog.Infrastructure/Gemini/GeminiApiClient.cs`
- Email sending: `AiWebSiteWatchDog.Infrastructure/Email/EmailSender.cs`
- Google auth/token handling: `AiWebSiteWatchDog.Infrastructure/Auth/GoogleCredentialProvider.cs`
- Persistence: `AiWebSiteWatchDog.Infrastructure/Persistence/` and domain entities under `AiWebSiteWatchDog.Domain/Entities/`

### Configuration (appsettings.json & environment variables)
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

### Token storage behavior
| Mode | Trigger | Location | Notes |
|---|---|---|---|
| Filesystem | `Google:UseDbTokenStore` = false | Hashed folder under OS profile/config path | Simpler for single-instance dev runs |
| Database (encrypted) | `Google:UseDbTokenStore` = true | `GoogleOAuthTokens` table | Requires `GOOGLE_TOKENS_ENCRYPTION_KEY` to decrypt; recommended for multi-instance deployments |

### Logging
Serilog is configured in `appsettings.json`. The file sink is set to daily roll and retention; adjust `Serilog` section in `appsettings.json` or via env vars to change behavior.


### Generating an encryption key

The app can generate a secure AES-256 key and exit:

```pwsh
dotnet run --project .\AiWebSiteWatchDog.API -- --generate-encryption-key
```

The command prints a Base64 string you should store in a secret manager and set as `GOOGLE_TOKENS_ENCRYPTION_KEY`.

### Running locally in Windows

Set the required env vars and run:

```pwsh
$env:GOOGLE_CLIENT_SECRET_JSON = Get-Content -Raw .\client_secret.json
$env:ConnectionStrings__DefaultConnection = 'Data Source=AiWebSitewatchdog.db'
dotnet run --project .\AiWebSiteWatchDog.API
```

Once running:
- User interface: http://localhost:5050
- Swagger UI: http://localhost:5050/swagger
- Health: http://localhost:5050/health
- Hangfire Dashboard: http://localhost:5050/hangfire

### Running using Docker compose
This repo includes a `docker-compose.yml` that wires sensible defaults:
- Maps port 8080 → 8080
- Persists data and logs to named volumes (`app_data`, `app_logs`)
- Sets both EF Core and Hangfire to use the same SQLite DB at `/data/app.db`
- Enables console logging (visible via `docker compose logs`)

Basic usage:
```pwsh
# Load client secret JSON content into an env var
$env:GOOGLE_CLIENT_SECRET_JSON = Get-Content -Raw .\client_secret.json

# Get encryption key
docker run --rm aiwebsitewatchdog --generate-encryption-key

# Save the printed Base64 value
$env:GOOGLE_TOKENS_ENCRYPTION_KEY = "<Base64-AES-256>"
```


```pwsh
# Build and start in the background
docker compose up -d --build

# Stream logs from the aiwebsitewatchdog container
docker compose logs -f aiwebsitewatchdog

# Stop and remove containers
docker compose down
```

Inspecting logs and data volumes:

```pwsh
# Tail app logs (console)
docker compose logs -f aiwebsitewatchdog

# List log files persisted on the volume
docker compose exec aiwebsitewatchdog ls -la /app/logs

# Copy logs to host
docker cp aiwebsitewatchdog:/app/logs .\logs-copy
```
