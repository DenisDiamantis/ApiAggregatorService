namespace ApiAggregatorService.Services.Statistics
{
	using ApiAggregatorService.Models.Statistics;
	using System.Collections.Concurrent;

	public class ApiStatisticsService : IApiStatisticsService
	{
		private readonly ConcurrentDictionary<string, ApiCallStats> _stats = new();

		public void Record(string apiName, double durationMs)
		{
			var stats = _stats.GetOrAdd(apiName, new ApiCallStats());

			lock (stats)
			{
				stats.TotalRequests++;
				stats.TotalDurationMs += durationMs;

				if (durationMs < 200)
					stats.FastCount++;
				else if (durationMs < 500)
					stats.MediumCount++;
				else
					stats.SlowCount++;
			}
		}

		public ApiStatisticsResponse GetStatistics()
		{
			return new ApiStatisticsResponse
			{
				ApiStats = _stats.ToDictionary(x => x.Key, x => x.Value)
			};
		}
	}

}
