using HordeServer.Plugins;
using MailKit.Security;

namespace OutOfTheBoxPlugins.Horde.EmailNotifications;

public class EmailNotificationsServerConfig : PluginServerConfig
{
	/// <summary>
	/// SMTP connection and sender settings.
	///</summary>
	public SmtpConfig Smtp { get; set; } = new();

	/// <summary>
	/// Per-notification-type recipients
	/// </summary>
	public RecipientsConfig Recipients { get; set; } = new();
}

public class SmtpConfig
{
	/// <summary>
	/// SMTP server hostname.
	/// </summary>
	public string? Host { get; set; }

	/// <summary>
	/// SMTP port. 587 = STARTTLS (default), 465 = implicit TLS, 25 = unencrypted relay.
	/// </summary>
	public int Port { get; set; } = 587;

	/// <summary>
	/// TLS mode. <see cref="SecureSocketOptions.StartTls"/> (default), <see cref="SecureSocketOptions.SslOnConnect"/> (port 465), or <see cref="SecureSocketOptions.None"/>.
	/// </summary>
	public SecureSocketOptions SslMode { get; set; } = SecureSocketOptions.StartTls;

	/// <summary>
	/// SMTP username.
	/// </summary>
	public string? Username { get; set; }

	/// <summary>
	/// SMTP password (or App Password for Gmail).
	/// </summary>
	public string? Password { get; set; }

	/// <summary>
	/// Email address used in the From header.
	/// </summary>
	public string FromAddress { get; set; } = "horde@example.com";

	/// <summary>
	/// Display name used in the From header.
	/// </summary>
	public string FromName { get; set; } = "Horde";
}

public class RecipientsConfig
{
	/// <summary>
	/// Config update failure notifications. Falls back from UpdateStreamsNotification when that list is empty.
	/// </summary>
	public string[] ConfigNotification { get; set; } = [];

	/// <summary>
	/// Stream config update failure notifications.
	/// </summary>
	public string[] UpdateStreamsNotification { get; set; } = [];

	/// <summary>
	/// Stream-level job notifications. Also the fallback when a step has no users with a known email.
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
