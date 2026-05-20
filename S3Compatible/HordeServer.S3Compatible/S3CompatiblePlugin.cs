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
	public void Configure(IApplicationBuilder app) { }

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<IObjectStoreFactory>(sp =>
			new S3CompatibleObjectStoreFactory(sp, sp.GetRequiredService<IOptionsMonitor<S3CompatibleGlobalConfig>>(), sp.GetRequiredService<ILogger<S3CompatibleObjectStoreFactory>>()));
	}
}
