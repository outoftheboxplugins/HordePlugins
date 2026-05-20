using System.Reflection;
using HordeServer.Plugins;
using HordeServer.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OutOfTheBoxPlugins.Horde.S3Compatible;

[Plugin("S3Compatible", DependsOn = ["Storage"], GlobalConfigType = typeof(S3CompatibleGlobalConfig))]
public class S3CompatiblePlugin : IPluginStartup
{
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<S3CompatibleObjectStoreFactory>();
	}

	public void Configure(IApplicationBuilder app)
	{
		ILogger<S3CompatiblePlugin> logger = app.ApplicationServices.GetRequiredService<ILogger<S3CompatiblePlugin>>();

		FieldInfo? field = typeof(StorageService).GetField("_objectStoreFactory", BindingFlags.NonPublic | BindingFlags.Instance);
		if (field == null)
		{
			logger.LogWarning("StorageService._objectStoreFactory field not found; Cannot override factory.");
			return;
		}

		StorageService storageService = app.ApplicationServices.GetRequiredService<StorageService>();
		S3CompatibleObjectStoreFactory s3Factory = app.ApplicationServices.GetRequiredService<S3CompatibleObjectStoreFactory>();
		field.SetValue(storageService, s3Factory);

		logger.LogInformation("Swapped StorageService._objectStoreFactory to S3CompatibleObjectStoreFactory.");
	}
}
