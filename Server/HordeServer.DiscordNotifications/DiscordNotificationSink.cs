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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text;

namespace OutOfTheBoxPlugins.Horde.DiscordNotifications;

public sealed class DiscordNotificationSink : INotificationSink
{
	readonly WebhooksConfig _webhooks;
	readonly IHttpClientFactory _httpClientFactory;
	readonly ILogger<DiscordNotificationSink> _logger;

	public DiscordNotificationSink(IOptions<DiscordNotificationsServerConfig> config, IHttpClientFactory httpClientFactory, ILogger<DiscordNotificationSink> logger)
	{
		_webhooks = config.Value.Webhooks;
		_httpClientFactory = httpClientFactory;
		_logger = logger;
	}

	async Task PostAsync(string webhookUrl, string content, CancellationToken cancellationToken)
	{
		using HttpClient client = _httpClientFactory.CreateClient();
		using HttpResponseMessage response = await client.PostAsJsonAsync(webhookUrl, new { content }, cancellationToken);
		response.EnsureSuccessStatusCode();
	}

	async Task TryPostAsync(string webhookUrl, string content, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(webhookUrl))
		{
			_logger.LogWarning("Skipping Discord notification: webhook URL is empty");
			return;
		}

		try
		{
			await PostAsync(webhookUrl, content, cancellationToken);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Failed to post Discord notification to webhook (content preview: {Preview})", content[..Math.Min(50, content.Length)]);
		}
	}

	async Task TryPostToAllAsync(string[] webhookUrls, string content, CancellationToken cancellationToken)
	{
		foreach (string url in webhookUrls)
			await TryPostAsync(url, content, cancellationToken);
	}

	/// <inheritdoc/>
	public Task NotifyJobScheduledAsync(List<JobScheduledNotification> notifications, CancellationToken cancellationToken)
		=> Task.CompletedTask;

	/// <inheritdoc/>
	public async Task NotifyJobCompleteAsync(IJob job, IGraph graph, LabelOutcome outcome, CancellationToken cancellationToken)
	{
		string content = $"**[Horde] Job '{job.Name}' {FormatLabelOutcome(outcome)}**\nJob `{job.Id}` in stream `{job.StreamId}` has {FormatLabelOutcome(outcome)}.";
		await TryPostToAllAsync(_webhooks.JobNotification, content, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task NotifyJobCompleteAsync(IUser user, IJob job, IGraph graph, LabelOutcome outcome, CancellationToken cancellationToken)
	{
		string content = $"**[Horde] Job '{job.Name}' {FormatLabelOutcome(outcome)}**\nHi {user.Name}, job `{job.Id}` in stream `{job.StreamId}` has {FormatLabelOutcome(outcome)}.";
		await TryPostToAllAsync(_webhooks.JobNotification, content, cancellationToken);
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
		if (info.Exception != null)
		{
			await NotifyConfigUpdateFailureAsync(info.Exception.Message, "-", cancellationToken: cancellationToken);
			return;
		}

		var sb = new StringBuilder();
		sb.AppendLine("**[Horde] Config Updated**");
		sb.AppendLine("Configuration updated successfully.");
		foreach (string line in info.Status)
			sb.AppendLine(line);

		await TryPostToAllAsync(_webhooks.ConfigNotification, sb.ToString(), cancellationToken);
	}

	/// <inheritdoc/>
	public async Task NotifyConfigUpdateFailureAsync(string errorMessage, string fileName, int? change = null, IUser? author = null, string? description = null, CancellationToken cancellationToken = default)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"**[Horde] Config Update Failed — {fileName}**");
		sb.AppendLine($"Change:      {change?.ToString() ?? "-"}");
		sb.AppendLine($"Author:      {author?.Name ?? "-"} ({author?.Login ?? "-"})");
		sb.AppendLine($"Description: {description ?? "-"}");
		sb.AppendLine();
		sb.AppendLine($"Error: {errorMessage}");

		await TryPostToAllAsync(_webhooks.ConfigNotification, sb.ToString(), cancellationToken);
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
