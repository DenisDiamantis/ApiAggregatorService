namespace ApiAggregatorService.Services.Statistics
{
	public class ApiPerformanceTracker
	{
		private readonly IApiStatisticsService _stats;

		public ApiPerformanceTracker(IApiStatisticsService stats)
		{
			_stats = stats;
		}

		public async Task<T> TrackAsync<T>(string apiName, Func<Task<T>> action)
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();

			try
			{
				return await action();
			}
			finally
			{
				sw.Stop();
				_stats.Record(apiName, sw.Elapsed.TotalMilliseconds);
			}
		}

	}

}
