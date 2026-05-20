using HordeServer.Plugins;
using HordeServer.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
		S3CompatibleStorageHacks.SwapObjectStoreFactory(app);
		S3CompatibleStorageHacks.ResetCacheOnConfigChange(app);
	}
}
