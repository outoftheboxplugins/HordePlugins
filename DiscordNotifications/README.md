# Discord Notifications

Sends build notifications to Discord channels via webhooks.

## Install

### Server

```
cd Engine\Source\Programs\Horde\HordeServer
dotnet add HordeServer.csproj package OutOfTheBoxPlugins.HordeDiscordNotifications
```

## Configuration

Add a `discordNotifications` block under `plugins.build` in `globals.json`:

```jsonc
{
  "plugins": {
    "build": {
      "discordNotifications": {
        "webhookUrl": "https://discord.com/api/webhooks/..."
      }
    }
  }
}
```
