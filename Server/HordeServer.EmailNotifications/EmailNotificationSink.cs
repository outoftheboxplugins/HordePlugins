using EpicGames.Horde.Jobs;
using EpicGames.Horde.Jobs.Graphs;
using HordeServer.Agents;
using HordeServer.Configuration;
using HordeServer.Devices;
using HordeServer.Issues;
using HordeServer.Logs;
using HordeServer.Notifications;
using HordeServer.Streams;
using HordeServer.Users;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text;

namespace OutOfTheBoxPlugins.Horde.EmailNotifications;

public sealed class EmailNotificationSink : INotificationSink
{
	readonly SmtpConfig _smtp;
	readonly RecipientsConfig _recipients;
	readonly ILogger<EmailNotificationSink> _logger;

	public EmailNotificationSink(IOptions<EmailNotificationsServerConfig> config, ILogger<EmailNotificationSink> logger)
	{
		_smtp = config.Value.Smtp;
		_recipients = config.Value.Recipients;
		_logger = logger;
	}

	async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken)
	{
		var message = new MimeMessage();
		message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromAddress));
		message.To.Add(MailboxAddress.Parse(to));
		message.Subject = subject;
		message.Body = new TextPart("plain") { Text = body };

		using var client = new SmtpClient();
		await client.ConnectAsync(_smtp.Host!, _smtp.Port, _smtp.SslMode, cancellationToken);
		await client.AuthenticateAsync(_smtp.Username ?? string.Empty, _smtp.Password ?? string.Empty, cancellationToken);
		await client.SendAsync(message, cancellationToken);
		await client.DisconnectAsync(true, cancellationToken);
	}

	async Task TrySendAsync(string? to, string subject, string body, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(to))
		{
			_logger.LogWarning("Skipping email notification (subject: {Subject}): recipient address is empty", subject);
			return;
		}

		try
		{
			await SendEmailAsync(to, subject, body, cancellationToken);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Failed to send email notification to {Address} (subject: {Subject})", to, subject);
		}
	}

	async Task TrySendToAllAsync(string[] addresses, string subject, string body, CancellationToken cancellationToken)
	{
		foreach (string address in addresses)
			await TrySendAsync(address, subject, body, cancellationToken);
	}

	/// <inheritdoc/>
	public Task NotifyJobScheduledAsync(List<JobScheduledNotification> notifications, CancellationToken cancellationToken)
		=> Task.CompletedTask;

	/// <inheritdoc/>
	public async Task NotifyJobCompleteAsync(IJob job, IGraph graph, LabelOutcome outcome, CancellationToken cancellationToken)
	{
		string subject = $"[Horde] Job '{job.Name}' {FormatLabelOutcome(outcome)}";
		string body = $"Job '{job.Name}' (ID: {job.Id}) in stream '{job.StreamId}' has {FormatLabelOutcome(outcome)}.";
		await TrySendToAllAsync(_recipients.JobNotification, subject, body, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task NotifyJobCompleteAsync(IUser user, IJob job, IGraph graph, LabelOutcome outcome, CancellationToken cancellationToken)
	{
		string subject = $"[Horde] Job '{job.Name}' {FormatLabelOutcome(outcome)}";
		string body = $"Hi {user.Name},\n\nJob '{job.Name}' (ID: {job.Id}) in stream '{job.StreamId}' has {FormatLabelOutcome(outcome)}.";
		await TrySendAsync(user.Email, subject, body, cancellationToken);
	}

	/// <inheritdoc/>
	public Task NotifyJobStepAbortedAsync(IEnumerable<IUser>? usersToNotify, IJob job, IJobStepBatch batch, IJobStep step, INode node, List<ILogEventData> jobStepEventData, CancellationToken cancellationToken)
		=> Task.CompletedTask;

	/// <inheritdoc/>
	public Task NotifyJobStepCompleteAsync(IEnumerable<IUser>? usersToNotify, IJob job, IJobStepBatch batch, IJobStep step, INode node, List<ILogEventData> jobStepEventData, CancellationToken cancellationToken)
		=> Task.CompletedTask;

	/// <inheritdoc/>
	public Task NotifyLabelCompleteAsync(IUser user, IJob job, ILabel label, int labelIdx, LabelOutcome outcome, List<(string, JobStepOutcome, Uri)> stepData, CancellationToken cancellationToken)
		=> Task.CompletedTask;

	/// <inheritdoc/>
	public Task NotifyIssueUpdatedAsync(IIssue issue, CancellationToken cancellationToken)
		=> Task.CompletedTask;

	/// <inheritdoc/>
	public Task SendIssueReportAsync(IssueReportGroup report, CancellationToken cancellationToken)
		=> Task.CompletedTask;

	/// <inheritdoc/>
	public async Task NotifyConfigUpdateAsync(ConfigUpdateInfo info, CancellationToken cancellationToken)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"Configuration '{info.FileName}' was updated successfully.");

		string subject = $"[Horde] Config Updated — {info.FileName}";
		await TrySendToAllAsync(_recipients.ConfigNotification, subject, sb.ToString(), cancellationToken);
	}

	/// <inheritdoc/>
	public async Task NotifyConfigUpdateFailureAsync(string errorMessage, string fileName, int? change = null, IUser? author = null, string? description = null, CancellationToken cancellationToken = default)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"Failed to update configuration: {fileName}");
		sb.AppendLine($"Change:      {change?.ToString() ?? "-"}");
		sb.AppendLine($"Author:      {author?.Name ?? "-"} ({author?.Login ?? "-"})");
		sb.AppendLine($"Description: {description ?? "-"}");
		sb.AppendLine();
		sb.AppendLine($"Error: {errorMessage}");

		string subject = $"[Horde] Config Update Failed — {fileName}";
		await TrySendToAllAsync(_recipients.ConfigNotification, subject, sb.ToString(), cancellationToken);
	}

	/// <inheritdoc/>
	public Task NotifyDeviceServiceAsync(string message, IDevice? device = null, IDevicePool? pool = null, StreamConfig? streamConfig = null, IJob? job = null, IJobStep? step = null, INode? node = null, IUser? user = null, CancellationToken cancellationToken = default)
		=> Task.CompletedTask;

	/// <inheritdoc/>
	public Task SendDeviceIssueReportAsync(DeviceIssueReport report, CancellationToken cancellationToken)
		=> Task.CompletedTask;

	/// <inheritdoc/>
	public Task SendAgentReportAsync(AgentReport report, CancellationToken cancellationToken)
		=> Task.CompletedTask;

	static string FormatLabelOutcome(LabelOutcome outcome) => outcome switch
	{
		LabelOutcome.Success => "Succeeded",
		LabelOutcome.Warnings => "Completed with Warnings",
		LabelOutcome.Failure => "Failed",
		_ => outcome.ToString(),
	};
}
