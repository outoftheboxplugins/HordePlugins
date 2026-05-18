using EpicGames.Horde.Acls;
using EpicGames.Horde.Tools;
using HordeServer.Tools;
using Microsoft.AspNetCore.Mvc;

namespace OutOfTheBoxPlugins.Horde.ToolsUploader;

[ApiController]
[Route("api/v1/tools-uploader")]
public class ToolsUploaderController(IToolCollection toolCollection) : ControllerBase
{
	[HttpGet("uploadable-tools")]
	public async Task<ActionResult<List<GetToolSummaryResponse>>> GetUploadableToolsAsync(CancellationToken cancellationToken)
	{
		IReadOnlyList<ITool> tools = await toolCollection.GetAllAsync(cancellationToken);

		List<GetToolSummaryResponse> result = new List<GetToolSummaryResponse>();
		foreach (ITool tool in tools)
		{
			bool toolAllowsManualUpload = tool.Metadata.GetValueOrDefault("manualUpload") == "true";
			bool userIsAllowedToUpload = tool.Authorize(ToolAclAction.UploadTool, User);
			if (toolAllowsManualUpload && userIsAllowedToUpload)
			{
				result.Add(ToSummaryResponse(tool));
			}
		}

		return result;
	}

	// Mirrors ToolsController.CreateGetToolSummaryResponse (HordeServer.Tools/Tools/ToolsController.cs) which is private static and unreachable from outside the assembly.
	static GetToolSummaryResponse ToSummaryResponse(ITool tool)
	{
		IToolDeployment? deployment = tool.Deployments.Count == 0 ? null : tool.Deployments[^1];
		return new GetToolSummaryResponse(tool.Id, tool.Name, tool.Description, tool.Category, tool.Group, tool.Platforms?.ToList(), deployment?.Version, deployment?.Id, deployment?.State, deployment?.Progress, tool.Bundled, tool.ShowInUgs, tool.ShowInDashboard, tool.ShowInToolbox, new Dictionary<string, string>(tool.Metadata, StringComparer.OrdinalIgnoreCase));
	}
}
