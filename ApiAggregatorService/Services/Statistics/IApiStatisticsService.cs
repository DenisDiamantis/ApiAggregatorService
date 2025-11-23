using ApiAggregatorService.Models.Statistics;

namespace ApiAggregatorService.Services.Statistics
{
	public interface IApiStatisticsService
	{
		void Record(string apiName, double durationMs);
		ApiStatisticsResponse GetStatistics();
	}

}
