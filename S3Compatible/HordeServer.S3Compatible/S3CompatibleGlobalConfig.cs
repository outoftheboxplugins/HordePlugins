using EpicGames.Horde.Storage;
using HordeServer.Plugins;
using HordeServer.Storage;
using Microsoft.Extensions.Logging;

namespace OutOfTheBoxPlugins.Horde.S3Compatible;

public class S3CompatibleGlobalConfig : IPluginConfig
{
	public List<S3CompatibleBackendEntry> Backends { get; set; } = [];

	public List<S3CompatibleNamespaceEntry> Namespaces { get; set; } = [];

	public void PostLoad(PluginConfigOptions configOptions)
	{
		var logger = configOptions.Logger;
		StorageConfig? storageConfig = configOptions.Plugins.OfType<StorageConfig>().FirstOrDefault();

		if (storageConfig == null)
		{
			logger?.LogWarning("StorageConfig not found; Cannot inject S3-compatible backends");
			return;
		}

		// Since Storage's PostLoad already ran we need to clear the namespace ACLs it registered before re-running it.
		// Also, it needs to run before namespaces are changed below because replaced namespaces become unreachable after.
		foreach (NamespaceConfig ns in storageConfig.Namespaces)
		{
			configOptions.ParentAcl.Children?.Remove(ns.Acl);
		}

		foreach (S3CompatibleBackendEntry entry in Backends)
		{
			BackendId backendId = new(entry.Id);

			BackendConfig? existing = storageConfig.Backends.FirstOrDefault(b => b.Id == backendId);
			if (existing != null)
			{
				storageConfig.Backends.Remove(existing);
				logger?.LogInformation("Overriding existing backend {BackendId} with S3-compatible backend", backendId);
			}
			else
			{
				logger?.LogInformation("Registering S3-compatible backend {BackendId}", backendId);
			}

			storageConfig.Backends.Add(new BackendConfig { Id = backendId });
		}

		foreach (S3CompatibleNamespaceEntry entry in Namespaces)
		{
			NamespaceId namespaceId = new(entry.Id);
			BackendId backendId = new(entry.Backend);

			NamespaceConfig? existing = storageConfig.Namespaces.FirstOrDefault(n => n.Id == namespaceId);
			if (existing != null)
			{
				storageConfig.Namespaces.Remove(existing);
				logger?.LogInformation("Overriding existing namespace {NamespaceId} with S3-compatible backend {BackendId}", namespaceId, backendId);
			}
			else
			{
				logger?.LogInformation("Registering S3-compatible namespace {NamespaceId} -> {BackendId}", namespaceId, backendId);
			}

			storageConfig.Namespaces.Add(new NamespaceConfig { Id = namespaceId, Backend = backendId });
		}

		storageConfig.PostLoad(configOptions);
	}
}

public class S3CompatibleNamespaceEntry
{
	/// <summary>
	/// Identifier for this namespace
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Backend to use for this namespace
	/// </summary>
	public string Backend { get; set; } = string.Empty;
}

public class S3CompatibleBackendEntry
{
	/// <summary>
	/// The storage backend ID
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// S3-compatible service URL, e.g. https://s3.us-east-1.amazonaws.com or https://fsn1.your-objectstorage.com
	/// </summary>
	public string Endpoint { get; set; } = string.Empty;

	/// <summary>
	/// Name of the bucket to use
	/// </summary>
	public string BucketName { get; set; } = string.Empty;

	/// <summary>
	/// Region to connect to
	/// </summary>
	public string Region { get; set; } = string.Empty;

	/// <summary>
	/// Use path-style URLs. Required for MinIO and most self-hosted S3 services.
	/// </summary>
	public bool ForcePathStyle { get; set; } = true;

	/// <summary>
	/// Access key ID.
	/// </summary>
	public string AccessKey { get; set; } = string.Empty;

	/// <summary>
	/// Secret access key.
	/// </summary>
	public string SecretKey { get; set; } = string.Empty;
}
