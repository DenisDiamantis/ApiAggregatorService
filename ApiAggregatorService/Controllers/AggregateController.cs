using ApiAggregatorService.Services.Aggregation;

namespace ApiAggregatorService.Controllers
{
	using ApiAggregatorService.Models.Enums;
	using Microsoft.AspNetCore.Authorization;
	using Microsoft.AspNetCore.Mvc;

	[ApiController]
	[Route("api/aggregate")]
	public class AggregateController : ControllerBase
	{
		private readonly IAggregatorService _aggregator;

		public AggregateController(IAggregatorService aggregator)
		{
			_aggregator = aggregator;
		}

		/// <summary>
		/// GET /api/aggregate?city=London&githubUser=dotnet&sortBy=stars&ascending=false&limit=3
		/// Requires Authorization: Bearer token
		/// </summary>
		[Authorize]
		[HttpGet]
		public async Task<IActionResult> Get(
			[FromQuery] string cityWeather,
			[FromQuery] NewsCategory category,
			[FromQuery] string githubUser,
			[FromQuery] RepoSortMode? repoSort,
			[FromQuery] bool ascending = false,
			[FromQuery] int limit = 3,
			CancellationToken ct = default)
		{
			if (limit <= 0)
				return BadRequest("Limit must be greater than 0.");

			var data = await _aggregator.AggregateAsync(cityWeather, category, githubUser, repoSort, ascending, limit, ct);
			return Ok(data);
		}
	}


}
