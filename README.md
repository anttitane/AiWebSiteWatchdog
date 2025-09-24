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
3. In the left menu, go to "APIs & Services" > "Library" and search for "Gmail API". Click "Enable".
4. Go to "APIs & Services" > "Credentials".
5. Click "Create Credentials" > "OAuth client ID".
6. If prompted, configure the consent screen (fill in required fields).
7. Choose "Desktop app" (for local testing) or "Web application" (for server).
8. Enter a name and click "Create".
9. Download the `client_secret.json` file.
10. Open the file and copy its contents. Paste it into the `GmailClientSecretJson` field in your app's email settings (via frontend or API).

**Important:**
- If your OAuth consent screen is in testing mode, you must add your Google account as a test user in the "Test users" section of the consent screen (found in the audience tab).
- Alternatively, you can publish the app to make it available to all users in your organization or publicly.
