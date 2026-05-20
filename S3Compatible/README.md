# S3Compatible — S3-compatible storage backend for Horde

Adds support for any S3-compatible object store (MinIO, Backblaze B2, Cloudflare R2, Wasabi, Hetzner Object Storage, etc.) as a Horde storage backend.

## Setup

Everything is configured in `globals.json` — no changes to `appsettings.json` are needed.

### 1. Declare your backends under `plugins.s3compatible`

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
      ]
    }
  }
}
```

### 2. Map namespaces to your backends under `plugins.storage`

```json
{
  "plugins": {
    "storage": {
      "namespaces": [
        { "id": "horde-tools", "backend": "backblaze" }
      ]
    }
  }
}
```

The `backend` value must match an `id` declared in `plugins.s3compatible.backends`.
