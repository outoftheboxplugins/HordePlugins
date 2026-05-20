using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using EpicGames.Core;
using EpicGames.Horde.Storage;
using HordeServer.Storage;
using HordeServer.Storage.ObjectStores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OutOfTheBoxPlugins.Horde.S3Compatible;

sealed class S3CompatibleObjectStoreFactory : IObjectStoreFactory
{
	readonly IServiceProvider _serviceProvider;
	readonly IOptionsMonitor<S3CompatibleGlobalConfig> _monitor;
	readonly object _lockObject = new object();
	IReadOnlyDictionary<IoHash, IObjectStore> _objectStores = new Dictionary<IoHash, IObjectStore>();
	readonly ILogger _logger;

	public S3CompatibleObjectStoreFactory(IServiceProvider serviceProvider, IOptionsMonitor<S3CompatibleGlobalConfig> monitor, ILogger<S3CompatibleObjectStoreFactory> logger)
	{
		_serviceProvider = serviceProvider;
		_monitor = monitor;
		_logger = logger;
	}

	public IObjectStore CreateObjectStore(BackendConfig config)
	{
		string id = config.Id.ToString();
		S3CompatibleBackendEntry? entry = _monitor.CurrentValue.Backends.FirstOrDefault(b => b.Id == id);
		if (entry == null)
		{
			_logger.LogDebug("Backend {Id} is not an S3-compatible backend, passing through to the default factory.", config.Id);
			return _serviceProvider.GetServices<IObjectStoreFactory>().First().CreateObjectStore(config);
		}

		IoHash hash = IoHash.Compute(JsonSerializer.SerializeToUtf8Bytes(entry));

		IObjectStore? objectStore;
		if (!_objectStores.TryGetValue(hash, out objectStore))
		{
			lock (_lockObject)
			{
				if (!_objectStores.TryGetValue(hash, out objectStore))
				{
					objectStore = CreateObjectStoreInternal(entry);

					Dictionary<IoHash, IObjectStore> newObjectStores = new Dictionary<IoHash, IObjectStore>(_objectStores);
					newObjectStores.Add(hash, objectStore);
					_objectStores = newObjectStores;

					_logger.LogInformation("Created S3-compatible object store {Id}@{Hash}", config.Id, hash);
				}
			}
		}

		return objectStore;
	}

	IObjectStore CreateObjectStoreInternal(S3CompatibleBackendEntry entry)
	{
		// Construct the client directly with endpoint and credentials so we bypass AwsObjectStoreFactory,
		// which would otherwise require patching to support custom ServiceURLs.
		AmazonS3Config awsConfig = new() { ServiceURL = entry.Endpoint, ForcePathStyle = entry.ForcePathStyle };
		AmazonS3Client client = new(new BasicAWSCredentials(entry.AccessKey, entry.SecretKey), awsConfig);
		return new AwsObjectStore(client, new S3CompatibleOptions(entry), new SemaphoreSlim(16), _serviceProvider.GetRequiredService<ILogger<AwsObjectStore>>());
	}
}

sealed class S3CompatibleOptions : IAwsStorageOptions
{
	readonly S3CompatibleBackendEntry _entry;

	public S3CompatibleOptions(S3CompatibleBackendEntry entry) => _entry = entry;

	public AwsCredentialsType? AwsCredentials => null;
	public string? AwsBucketName => _entry.BucketName;
	public string? AwsBucketPath => null;
	public string? AwsRole => null;
	public string? AwsProfile => null;
	public string? AwsRegion => _entry.Region;
}
