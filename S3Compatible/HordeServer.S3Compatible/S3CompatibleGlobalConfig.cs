using HordeServer.Plugins;
using HordeServer.Storage;
using Microsoft.Extensions.Logging;

namespace OutOfTheBoxPlugins.Horde.S3Compatible;

public class S3CompatibleGlobalConfig : IPluginConfig
{
	public List<S3CompatibleBackendEntry> Backends { get; set; } = [];

	public void PostLoad(PluginConfigOptions configOptions)
	{
		var logger = configOptions.Logger;
		StorageConfig? storageConfig = configOptions.Plugins.OfType<StorageConfig>().FirstOrDefault();

		if (storageConfig == null)
		{
			logger?.LogWarning("S3Compatible plugin could not find StorageConfig; backends will not be registered.");
			return;
		}

		foreach (S3CompatibleBackendEntry entry in Backends)
		{
			BackendId backendId = new(entry.Id);
			if (storageConfig.Backends.All(b => b.Id != backendId))
			{
				logger?.LogInformation("Registering S3-compatible backend {BackendId}", backendId);
				storageConfig.Backends.Add(new BackendConfig { Id = backendId });
			}
		}

		storageConfig.PostLoad(configOptions);
	}
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
