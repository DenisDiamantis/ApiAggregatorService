using ApiAggregatorService.Models;
using ApiAggregatorService.Services.External;

namespace ApiAggregatorService.Services.Aggregation
{
	using ApiAggregatorService.Models.Enums;
	using ApiAggregatorService.Models.News;
	using ApiAggregatorService.Services.Statistics;

	public class AggregatorService : IAggregatorService
	{
		private readonly IWeatherService _weather;
		private readonly IGithubService _github;
		private readonly INewsService _news;
		private readonly ILogger<AggregatorService> _logger;

		public AggregatorService(
			IWeatherService weather,
			IGithubService github,
			INewsService news,
			ILogger<AggregatorService> logger)
		{
			_weather = weather;
			_github = github;
			_news = news;
			_logger = logger;

		}

		public async Task<AggregatedResponse> AggregateAsync(
			string cityWeather,
			NewsCategory category,
			string githubUser,
			RepoSortMode? repoSort,
			bool ascending = false,
			int limit = 3,
			CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(cityWeather))
				cityWeather = "London";
			if (string.IsNullOrWhiteSpace(githubUser))
				githubUser = "dotnet";

			var weatherTask = _weather.GetWeatherAsync(cityWeather);
			var newsTask = _news.GetLatestHeadlinesAsync(category);
			var githubTask = _github.GetUserReposAsync(githubUser, repoSort, ascending, limit);

			await Task.WhenAll(
				SafeWrap(weatherTask, "Weather"),
				SafeWrap(githubTask, "Githhub"),
				SafeWrap(newsTask, "News")
			);

			var weather = await TryGetResult(weatherTask);
			var github = await TryGetResult(githubTask) ?? new List<GithubRepo>();
			var news = await TryGetResult(newsTask) ?? new NewsResponse();

			IEnumerable<GithubRepo> repos = github;

			if (repoSort is not null)
			{
				repos = repoSort switch
				{
					RepoSortMode.Alphabetical =>
						ascending ? repos.OrderBy(r => r.Name)
								  : repos.OrderByDescending(r => r.Name),

					RepoSortMode.Stars =>
						ascending ? repos.OrderBy(r => r.Stars)
								  : repos.OrderByDescending(r => r.Stars),

					RepoSortMode.LastUpdated =>
						ascending ? repos.OrderBy(r => r.LastUpdated)
								  : repos.OrderByDescending(r => r.LastUpdated),

					_ => repos
				};
			}

			var repoList = (limit > 0)
				? repos.Take(limit).ToList()
				: repos.ToList();

			var articleList = (limit > 0)
				? news.Articles.Take(limit).ToList()
				: news.Articles;

			return new AggregatedResponse
			{
				Weather = weather,
				News = articleList,
				GithubRepos = repoList
			};
		}

		private async Task SafeWrap(Task t, string apiName)
		{
			try { await t; }
			catch (Exception ex)
			{
				_logger.LogWarning(
					ex,
					"{ApiName} API call failed during aggregation",
					apiName
				);
			}
		}

		private async Task<T?> TryGetResult<T>(Task<T> t)
		{
			try { return await t; }
			catch (Exception ex)
			{
				_logger.LogWarning(
					ex,
					"Failed to retrieve result for: {TypeName}",
					typeof(T).Name
				);
				return default;
			}
		}
	}
}
