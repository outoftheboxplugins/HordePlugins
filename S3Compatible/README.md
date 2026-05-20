# S3Compatible — S3-compatible storage backend for Horde

Adds support for any S3-compatible object store (MinIO, Backblaze B2, Cloudflare R2, Wasabi, Hetzner Object Storage, etc.) as a Horde storage backend.

## Setup

Everything is configured in `globals.json` — no changes to `appsettings.json` are needed.

Add both `backends` and `namespaces` under `plugins.s3compatible`:

```json
{
  "plugins": {
    "s3compatible": {
      "backends": [
        {
          "id": "backblaze",
          "endpoint": "https://s3.eu-central-003.backblazeb2.com",
          "bucketName": "my-horde-bucket",
          "region": "eu-central-003",
          "forcePathStyle": false,
          "accessKey": "your-key-id",
          "secretKey": "your-application-key"
        }
      ],
      "namespaces": [
        { "id": "horde-tools", "backend": "backblaze" }
      ]
    }
  }
}
```

> **Important:** Namespaces that point to S3-compatible backends must be declared under `plugins.s3compatible.namespaces`, not `plugins.storage.namespaces`. Storage validates its namespaces before this plugin has a chance to register the backends, which would cause a startup error.

### Optional: store objects under a path prefix

Use `bucketPath` to isolate Horde data within a shared bucket:

```json
{
  "id": "backblaze",
  ...
  "bucketPath": "horde-artifacts"
}
```

Nested paths are supported (e.g. `"horde-artifacts/prod"`).
