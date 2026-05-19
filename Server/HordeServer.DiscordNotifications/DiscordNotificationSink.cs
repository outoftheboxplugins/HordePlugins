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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OutOfTheBoxPlugins.Horde.DiscordNotifications;

class DiscordField
{
	[JsonPropertyName("name")]
	public required string Name { get; set; }

	[JsonPropertyName("value")]
	public required string Value { get; set; }

	[JsonPropertyName("inline")]
	public bool Inline { get; set; }
}

class DiscordEmbed
{
	[JsonPropertyName("title")]
	public string? Title { get; set; }

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[JsonPropertyName("color")]
	public int? Color { get; set; }

	[JsonPropertyName("fields")]
	public DiscordField[]? Fields { get; set; }
}

class DiscordPayload
{
	[JsonPropertyName("username")]
	public string? Username { get; set; }

	[JsonPropertyName("content")]
	public string? Content { get; set; }

	[JsonPropertyName("embeds")]
	public DiscordEmbed[]? Embeds { get; set; }
}

public sealed class DiscordNotificationSink : INotificationSink
{
	readonly WebhooksConfig _webhooks;
	readonly IHttpClientFactory _httpClientFactory;
	readonly ILogger<DiscordNotificationSink> _logger;

	const int ColorGreen = 0x2ECC71;
	const int ColorYellow = 0xF39C12;
	const int ColorRed = 0xE74C3C;
	const int ColorBlue = 0x3498DB;

	public DiscordNotificationSink(IOptions<DiscordNotificationsServerConfig> config, IHttpClientFactory httpClientFactory, ILogger<DiscordNotificationSink> logger)
	{
		_webhooks = config.Value.Webhooks;
		_httpClientFactory = httpClientFactory;
		_logger = logger;
	}

	async Task PostAsync(string webhookUrl, DiscordEmbed embed, CancellationToken cancellationToken)
	{
		var payload = new DiscordPayload { Username = "Horde", Embeds = [embed] };
		var jsonOptions = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
		using HttpClient client = _httpClientFactory.CreateClient();
		using HttpResponseMessage response = await client.PostAsJsonAsync(webhookUrl, payload, jsonOptions, cancellationToken);
		response.EnsureSuccessStatusCode();
	}

	async Task TryPostAsync(string webhookUrl, DiscordEmbed embed, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(webhookUrl))
		{
			_logger.LogWarning("Skipping Discord notification: webhook URL is empty");
			return;
		}

		try
		{
			await PostAsync(webhookUrl, embed, cancellationToken);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Failed to post Discord notification to webhook");
		}
	}

	async Task TryPostToAllAsync(string[] webhookUrls, DiscordEmbed embed, CancellationToken cancellationToken)
	{
		foreach (string url in webhookUrls)
			await TryPostAsync(url, embed, cancellationToken);
	}

	/// <inheritdoc/>
	public Task NotifyJobScheduledAsync(List<JobScheduledNotification> notifications, CancellationToken cancellationToken)
		=> Task.CompletedTask;

	/// <inheritdoc/>
	public async Task NotifyJobCompleteAsync(IJob job, IGraph graph, LabelOutcome outcome, CancellationToken cancellationToken)
	{
		var embed = new DiscordEmbed
		{
			Title = $"Job '{job.Name}' {FormatLabelOutcome(outcome)}",
			Color = OutcomeColor(outcome),
			Fields =
			[
				new DiscordField { Name = "Stream", Value = job.StreamId.ToString(), Inline = true },
				new DiscordField { Name = "Job ID", Value = job.Id.ToString(), Inline = true },
			]
		};

		await TryPostToAllAsync(_webhooks.JobNotification, embed, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task NotifyJobCompleteAsync(IUser user, IJob job, IGraph graph, LabelOutcome outcome, CancellationToken cancellationToken)
	{
		var embed = new DiscordEmbed
		{
			Title = $"Job '{job.Name}' {FormatLabelOutcome(outcome)}",
			Description = $"Hi {user.Name}",
			Color = OutcomeColor(outcome),
			Fields =
			[
				new DiscordField { Name = "Stream", Value = job.StreamId.ToString(), Inline = true },
				new DiscordField { Name = "Job ID", Value = job.Id.ToString(), Inline = true },
			]
		};

		await TryPostToAllAsync(_webhooks.JobNotification, embed, cancellationToken);
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
		sb.AppendLine("Configuration updated successfully.");
		foreach (string line in info.Status)
			sb.AppendLine(line);

		var embed = new DiscordEmbed
		{
			Title = "Config Updated",
			Description = sb.ToString(),
			Color = ColorGreen,
		};

		await TryPostToAllAsync(_webhooks.ConfigNotification, embed, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task NotifyConfigUpdateFailureAsync(string errorMessage, string fileName, int? change = null, IUser? author = null, string? description = null, CancellationToken cancellationToken = default)
	{
		var embed = new DiscordEmbed
		{
			Title = $"Config Update Failed — {fileName}",
			Color = ColorRed,
			Fields =
			[
				new DiscordField { Name = "Change", Value = change?.ToString() ?? "-", Inline = true },
				new DiscordField { Name = "Author", Value = author != null ? $"{author.Name} ({author.Login})" : "-", Inline = true },
				new DiscordField { Name = "Description", Value = description ?? "-" },
				new DiscordField { Name = "Error", Value = errorMessage },
			]
		};

		await TryPostToAllAsync(_webhooks.ConfigNotification, embed, cancellationToken);
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

	static int OutcomeColor(LabelOutcome outcome) => outcome switch
	{
		LabelOutcome.Success => ColorGreen,
		LabelOutcome.Warnings => ColorYellow,
		LabelOutcome.Failure => ColorRed,
		_ => ColorBlue,
	};
}
