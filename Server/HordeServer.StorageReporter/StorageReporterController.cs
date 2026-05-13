using EpicGames.Horde.Storage;
using HordeServer.Storage;
using Microsoft.AspNetCore.Mvc;

namespace OutOfTheBoxPlugins.Horde.StorageReporter;

[ApiController]
[Route("api/v1/storage-reporter")]
public class StorageReporterController(StorageService storageService) : ControllerBase
{
	[HttpGet("by-namespace")]
	public async Task<ActionResult<BreakdownResponse>> GetByNamespaceAsync(CancellationToken cancellationToken)
	{
		IStorageStats? stats = (await storageService.FindStatsAsync(count: 1, cancellationToken: cancellationToken)).FirstOrDefault();
		if (stats == null)
		{
			return new BreakdownResponse { GeneratedUtc = DateTime.UtcNow, Slices = Array.Empty<BreakdownSlice>() };
		}

		List<BreakdownSlice> slices = stats.Namespaces
			.OrderByDescending(kv => kv.Value.Size)
			.Select(kv => new BreakdownSlice { Label = kv.Key.ToString(), SizeBytes = kv.Value.Size, Count = kv.Value.Count })
			.ToList();

		return new BreakdownResponse { GeneratedUtc = stats.Time, Slices = slices };
	}
}

public record BreakdownSlice
{
	public required string Label { get; init; }
	public long SizeBytes { get; init; }
	public long Count { get; init; }
}

public record BreakdownResponse
{
	public DateTime GeneratedUtc { get; init; }
	public required IReadOnlyList<BreakdownSlice> Slices { get; init; }
}
