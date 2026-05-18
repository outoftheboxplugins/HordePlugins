using HordeServer.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace OutOfTheBoxPlugins.Horde.ToolsUploader;

[Plugin("ToolsUploader", DependsOn = ["Tools"])]
public class ToolsUploaderPlugin : IPluginStartup
{
	public void Configure(IApplicationBuilder app) { }
	public void ConfigureServices(IServiceCollection services) { }
}
