namespace ApiAggregatorService.Models.Statistics
{
	public class ApiCallStats
	{
		public int TotalRequests { get; set; }
		public double AverageResponseTimeMs => TotalRequests == 0 ? 0 : TotalDurationMs / TotalRequests;

		public double TotalDurationMs { get; set; }

		public int FastCount { get; set; }
		public int MediumCount { get; set; }
		public int SlowCount { get; set; }
	}

}
