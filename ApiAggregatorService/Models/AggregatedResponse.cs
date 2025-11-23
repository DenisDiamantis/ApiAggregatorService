using ApiAggregatorService.Models.News;

namespace ApiAggregatorService.Models
{
	public class AggregatedResponse
	{
		public WeatherResponse? Weather { get; init; }
		public List<NewsArticle> News { get; init; } = new();
		public List<GithubRepo> GithubRepos { get; init; } = new();
	}

}
