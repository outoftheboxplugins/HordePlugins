# Email Notifications

Sends build notifications via SMTP email.

## Install

### Server

```
cd Engine\Source\Programs\Horde\HordeServer
dotnet add HordeServer.csproj package OutOfTheBoxPlugins.HordeEmailNotifications
```

## Configuration

Add an `emailNotifications` block under `plugins.build` in `globals.json`:

```jsonc
{
  "plugins": {
    "build": {
      "emailNotifications": {
        "smtpHost": "smtp.example.com",
        "smtpPort": 587,
        "fromAddress": "horde@example.com"
      }
    }
  }
}
```
