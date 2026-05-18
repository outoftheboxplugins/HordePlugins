# Plugins

## Storage Reporter

Breaks down the artifacts distribution for CI.

<img width="864" height="405" alt="image" src="https://github.com/user-attachments/assets/ee8c9bd4-9641-4797-9e5b-96e60f475f41" />

### Server
```
cd Engine\Source\Programs\Horde\HordeServer
dotnet add HordeServer.csproj package
```

### Dashboard

```
cd Engine\Source\Programs\Horde\HordeDashboard
npx @outoftheboxplugins/horde-plugins-cli storage-reporter
```

## Tools Uploader

Adds a **Manual Upload** entry under the Tools menu that lets authorized users upload a new tool deployment (.zip) without needing CLI access.

<img width="1495" height="898" alt="image" src="https://github.com/user-attachments/assets/72ed82e2-eb96-47e2-9c56-ab980f539102" />

### Server
```
cd Engine\Source\Programs\Horde\HordeServer
dotnet add HordeServer.csproj package OutOfTheBoxPlugins.HordeToolsUploader
```

### Dashboard

```
cd Engine\Source\Programs\Horde\HordeDashboard
npx @outoftheboxplugins/horde-plugins-cli tools-uploader
```

### Configuration

NOTE: Tools must opt in via `"manualUpload": "true"` in their `metadata` block in `globals.json`, and the user must have the `UploadTool` ACL action on that tool.

In `globals.json`, add `"manualUpload": "true"` to the `metadata` of any tool you want to appear in the uploader:

```jsonc
{
  "plugins": {
    "tools": {
      "tools": [
        {
          "id": "my-tool",
          "name": "My Tool",
          "metadata": {
            "manualUpload": "true"
          }
        }
      ]
    }
  }
}
```
