namespace ApiAggregatorService.Models.Statistics
{
	public class ApiStatisticsResponse
	{
		public Dictionary<string, ApiCallStats> ApiStats { get; set; } = new();
	}

}
