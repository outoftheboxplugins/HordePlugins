using HordeServer.Notifications;
using HordeServer.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace OutOfTheBoxPlugins.Horde.EmailNotifications;

[Plugin("EmailNotifications", DependsOn = ["Build"], ServerConfigType = typeof(EmailNotificationsServerConfig))]
public class EmailNotificationsPlugin : IPluginStartup
{
	public void Configure(IApplicationBuilder app) { }

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<EmailNotificationSink>();
		services.AddSingleton<INotificationSink, EmailNotificationSink>(
			sp => sp.GetRequiredService<EmailNotificationSink>());
	}
}
