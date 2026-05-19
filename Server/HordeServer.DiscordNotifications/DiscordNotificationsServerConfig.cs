using HordeServer.Plugins;

namespace OutOfTheBoxPlugins.Horde.DiscordNotifications;

public class DiscordNotificationsServerConfig : PluginServerConfig
{
	/// <summary>
	/// Per-notification-type Discord webhook URLs.
	/// </summary>
	public WebhooksConfig Webhooks { get; set; } = new();
}

public class WebhooksConfig
{
	/// <summary>
	/// Config update notifications.
	/// </summary>
	public string[] ConfigNotification { get; set; } = [];

	/// <summary>
	/// Stream-level job notifications.
	/// </summary>
	public string[] JobNotification { get; set; } = [];

	/// <summary>
	/// Agent farm report notifications.
	/// </summary>
	public string[] AgentNotification { get; set; } = [];

	/// <summary>
	/// Device issue report notifications.
	/// </summary>
	public string[] DeviceReport { get; set; } = [];

	/// <summary>
	/// Issue update and issue report notifications.
	/// </summary>
	public string[] IssueNotification { get; set; } = [];
}
