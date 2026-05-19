using HordeServer.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace OutOfTheBoxPlugins.Horde.StorageReporter;

[Plugin("StorageReporter", DependsOn = ["Storage"])]
public class StorageReporterPlugin : IPluginStartup
{
	public void Configure(IApplicationBuilder app) { }
	public void ConfigureServices(IServiceCollection services) { }
}
