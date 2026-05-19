using HordeServer.Notifications;
using HordeServer.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace OutOfTheBoxPlugins.Horde.DiscordNotifications;

[Plugin("DiscordNotifications", DependsOn = ["Build"], ServerConfigType = typeof(DiscordNotificationsServerConfig))]
public class DiscordNotificationsPlugin : IPluginStartup
{
	public void Configure(IApplicationBuilder app) { }

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddHttpClient();
		services.AddSingleton<DiscordNotificationSink>();
		services.AddSingleton<INotificationSink, DiscordNotificationSink>(
			sp => sp.GetRequiredService<DiscordNotificationSink>());
	}
}
