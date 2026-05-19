# Storage Reporter

Breaks down the artifacts distribution for CI.

<img width="864" height="405" alt="image" src="https://github.com/user-attachments/assets/ee8c9bd4-9641-4797-9e5b-96e60f475f41" />

## Install

### Server

```
cd Engine\Source\Programs\Horde\HordeServer
dotnet add HordeServer.csproj package OutOfTheBoxPlugins.HordeStorageReporter
```

### Dashboard

```
cd Engine\Source\Programs\Horde\HordeDashboard
npx @outoftheboxplugins/horde-plugins-cli storage-reporter
```
