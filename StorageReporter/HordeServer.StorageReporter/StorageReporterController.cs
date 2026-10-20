using HordeServer.Server;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace OutOfTheBoxPlugins.Horde.StorageReporter;

[ApiController]
[Route("api/v1/storage-reporter")]
public class StorageReporterController(IMongoService mongoService) : ControllerBase
{
	[HttpGet("by-namespace")]
	public async Task<ActionResult<BreakdownResponse>> GetByNamespaceAsync(CancellationToken cancellationToken)
	{
		List<BsonDocument> stats = await mongoService.GetCollection<BsonDocument>("Storage.Blobs").Aggregate()
			.Group(new BsonDocument { { "_id", "$ns" }, { "size", new BsonDocument("$sum", "$len") }, { "count", new BsonDocument("$sum", 1) } })
			.ToListAsync(cancellationToken);

		List<BreakdownSlice> slices = stats
			.OrderByDescending(x => x["size"].ToInt64())
			.Select(x => new BreakdownSlice { Label = x["_id"].ToString()!, SizeBytes = x["size"].ToInt64(), Count = x["count"].ToInt64() })
			.ToList();

		return new BreakdownResponse { GeneratedUtc = DateTime.UtcNow, Slices = slices };
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
