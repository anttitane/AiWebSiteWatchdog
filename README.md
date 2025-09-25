# AiWebSiteWatchdog
AI powered .NET app which scans regularly predefined set of web sites and searches predefined interests

## How to get Google OAuth2 client_secret.json for Gmail API
This app uses the Gmail API via Google Cloud and OAuth2 for secure email delivery:

- **OAuth2 Security:** Your Gmail password is never seen or stored by the app. Instead, Google issues a secure, time-limited access token after you grant permission.
- **User Consent:** You must log in and explicitly approve access to your Gmail account. No email can be sent without your consent.
- **Google Cloud Controls:** All credentials are managed in Google Cloud Console, which provides strong security and access controls.
- **Token Storage:** Access tokens are stored securely and can be revoked by you at any time in your Google account settings.
- **API Scopes:** The app only requests the minimum required scope (`GmailSend`), limiting what it can do with your account.

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
10. Open the file and copy its contents. Set it as an environment variable `GOOGLE_CLIENT_SECRET_JSON` (see Environment Variables section below). The secret is no longer stored in the database for security reasons.

**Important:**
- If your OAuth consent screen is in testing mode, you must add your Google account as a test user in the "Test users" section of the consent screen (found in the audience tab).
- Alternatively, you can publish the app to make it available to all users in your organization or publicly.
- The app requests both Gmail send and Gemini (Generative Language) scopes using a single combined consent; if scopes change later you must re-consent (delete stored token row / token files).

---

## Environment Variables

The application uses environment variables for secrets and optional storage behavior. These should be set before starting the API.

| Variable | Required | Purpose | Notes |
|----------|----------|---------|-------|
| `GOOGLE_CLIENT_SECRET_JSON` | Yes | Raw contents of your Google OAuth client `client_secret.json` | Do NOT commit. Multi-line JSON accepted. |
| `USE_DB_TOKEN_STORE` | No (default `false`) | When `true`, persists encrypted OAuth tokens in the database (`GoogleOAuthTokens` table) | Use for multi-instance or container deployments. |
| `GOOGLE_TOKENS_ENCRYPTION_KEY` | Yes if DB store enabled | Base64 encoded AES key (16/24/32 bytes) used to encrypt token JSON at rest | Generate once and rotate carefully. |
| `GOOGLE_TOKENS_PATH` | Optional (filesystem mode only) | Override base directory for local token cache when not using DB | Useful in container mounts / dev sandbox. |

### Generating an Encryption Key
### Generate a Key via the Application (no openssl needed)
You can have the app produce a secure 32-byte (AES-256) Base64 key and exit:
```pwsh
dotnet run --project .\AiWebSiteWatchDog.API -- --generate-encryption-key
```
Output example:
```
Base64 AES-256 key (store in GOOGLE_TOKENS_ENCRYPTION_KEY):
q2m4YyI2M0qXq3F4v6Q8Yd9vKk1Jd3tXgE2p9lqV2mI=
Length (bytes): 32  | IMPORTANT: keep this stable across deployments.
```
Copy the printed value into your secret manager or set it directly as an environment variable.

#### Quick (Windows) set and run
```pwsh
$key = (dotnet run --project .\AiWebSiteWatchDog.API -- --generate-encryption-key | Select-Object -Skip 1 -First 1).Trim()
$env:GOOGLE_TOKENS_ENCRYPTION_KEY = $key
dotnet run --project .\AiWebSiteWatchDog.API
```

#### Generate inside Docker then run API with the key
```powershell
# 1. Generate key (prints 3 lines)
$out = docker run --rm aiwatchdog:latest --generate-encryption-key
$key = ($out -split "`n")[1].Trim()

# 2. Run container with key as env var
docker run --rm -p 5050:8080 `
	-e GOOGLE_CLIENT_SECRET_JSON="$(Get-Content -Raw .\client_secret.json)" `
	-e USE_DB_TOKEN_STORE=true `
	-e GOOGLE_TOKENS_ENCRYPTION_KEY=$key `
	aiwatchdog:latest
```


### Local Development (PowerShell)
```pwsh
$env:GOOGLE_CLIENT_SECRET_JSON = (Get-Content -Raw .\client_secret.json)
$env:USE_DB_TOKEN_STORE = 'true'            # optional
$env:GOOGLE_TOKENS_ENCRYPTION_KEY = 'Base64KeyHere'  # required if USE_DB_TOKEN_STORE=true
dotnet run --project .\AiWebSiteWatchDog.API
```

### Docker Run Example
```pwsh
docker run --rm -p 5050:8080 \
	-e GOOGLE_CLIENT_SECRET_JSON="$(Get-Content -Raw .\client_secret.json)" \
	-e USE_DB_TOKEN_STORE=true \
	-e GOOGLE_TOKENS_ENCRYPTION_KEY=Base64KeyHere \
	your-image:latest
```

### Docker Compose (`docker-compose.yml` snippet)
```yaml
services:
	aiwatchdog:
		image: your-image:latest
		ports:
			- "5050:8080"
		environment:
			GOOGLE_CLIENT_SECRET_JSON: ${GOOGLE_CLIENT_SECRET_JSON}
			USE_DB_TOKEN_STORE: "true"
			GOOGLE_TOKENS_ENCRYPTION_KEY: ${GOOGLE_TOKENS_ENCRYPTION_KEY}
```
Then in a `.env` file (not committed):
```
GOOGLE_CLIENT_SECRET_JSON={"installed":{...}}
GOOGLE_TOKENS_ENCRYPTION_KEY=Base64KeyHere
```

### Token Storage Behavior
| Mode | Trigger | Location | Encryption |
|------|---------|----------|------------|
| Filesystem | `USE_DB_TOKEN_STORE` unset / false | Hashed directory under OS config path | OS permissions only |
| DB Encrypted | `USE_DB_TOKEN_STORE=true` | Table `GoogleOAuthTokens` | AES-GCM using `GOOGLE_TOKENS_ENCRYPTION_KEY` |

To force re-consent (e.g., after adding a new scope) delete the row in `GoogleOAuthTokens` (DB mode) or remove the hashed token folder (filesystem mode) and trigger an email/gemini action again.

---
