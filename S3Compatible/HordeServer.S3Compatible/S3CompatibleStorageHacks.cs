using System.Reflection;
using HordeServer.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OutOfTheBoxPlugins.Horde.S3Compatible;

/// <summary>
/// Reflection-based workarounds for StorageService internals that have no public API.
/// </summary>
static class S3CompatibleStorageHacks
{
	/// <summary>
	/// Swaps StorageService's internal IObjectStoreFactory to our S3-compatible one.
	/// </summary>
	public static bool SwapObjectStoreFactory(IApplicationBuilder app)
	{
		ILogger logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(S3CompatibleStorageHacks));
		FieldInfo? field = typeof(StorageService).GetField("_objectStoreFactory", BindingFlags.NonPublic | BindingFlags.Instance);
		if (field == null)
		{
			logger.LogWarning("StorageService._objectStoreFactory field not found; Cannot override factory.");
			return false;
		}

		StorageService storageService = app.ApplicationServices.GetRequiredService<StorageService>();
		S3CompatibleObjectStoreFactory factory = app.ApplicationServices.GetRequiredService<S3CompatibleObjectStoreFactory>();
		field.SetValue(storageService, factory);
		logger.LogInformation("Swapped StorageService._objectStoreFactory to S3CompatibleObjectStoreFactory.");
		return true;
	}

	/// <summary>
	/// Resets StorageService's cached state when our config changes so updated settings take effect without a restart.
	/// </summary>
	public static bool ResetCacheOnConfigChange(IApplicationBuilder app)
	{
		ILogger logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(S3CompatibleStorageHacks));
		FieldInfo? stateField = typeof(StorageService).GetField("_lastState", BindingFlags.NonPublic | BindingFlags.Instance);
		FieldInfo? lockField = typeof(StorageService).GetField("_lockObject", BindingFlags.NonPublic | BindingFlags.Instance);
		if (stateField == null || lockField == null)
		{
			logger.LogWarning("StorageService state fields not found; config changes will require a restart to take effect.");
			return false;
		}

		StorageService storageService = app.ApplicationServices.GetRequiredService<StorageService>();
		IOptionsMonitor<S3CompatibleGlobalConfig> monitor = app.ApplicationServices.GetRequiredService<IOptionsMonitor<S3CompatibleGlobalConfig>>();
		object lockObj = lockField.GetValue(storageService)!;
		monitor.OnChange(_ =>
		{
			lock (lockObj) { stateField.SetValue(storageService, null); }
			logger.LogInformation("S3-compatible config changed; storage state reset.");
		});
		return true;
	}
}
