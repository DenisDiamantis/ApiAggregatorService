using ApiAggregatorService.Services.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregatorService.Controllers
{
	[Authorize]
	[ApiController]
	[Route("api/stats")]
	public class StatisticsController : ControllerBase
	{
		private readonly IApiStatisticsService _stats;

		public StatisticsController(IApiStatisticsService stats)
		{
			_stats = stats;
		}

		[HttpGet]
		public IActionResult Get()
		{
			return Ok(_stats.GetStatistics());
		}
	}

}
