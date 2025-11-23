using ApiAggregatorService.Models;
using ApiAggregatorService.Models.Enums;
using ApiAggregatorService.Services.Cache;
using ApiAggregatorService.Services.Statistics;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ApiAggregatorService.Services.External
{
	public class GithubService : IGithubService
	{
		private readonly HttpClient _http;
		private readonly ApiPerformanceTracker _tracker;
		private readonly IApiCacheService _cache;
		private readonly ILogger<GithubService> _logger;

		public GithubService(
			IHttpClientFactory factory,
			ApiPerformanceTracker tracker,
			IApiCacheService cache,
			ILogger<GithubService> logger)
		{
			_http = factory.CreateClient("GitHub");
			_http.DefaultRequestHeaders.UserAgent.ParseAdd("ApiAggregatorService/1.0");
			_tracker = tracker;
			_cache = cache;
			_logger = logger;
		}

		public async Task<List<GithubRepo>> GetUserReposAsync(
			string username,
			RepoSortMode? sort = null,
			bool ascending = false,
			int limit = 10,
			CancellationToken ct = default)
		{
			var cacheKey = $"github_repos_{username}_{sort}_{ascending}_{limit}";
			var ttl = TimeSpan.FromMinutes(10);

			try
			{
				return await _cache.GetOrSetAsync(
					cacheKey,
					() => FetchAndTrackAsync(username, sort, ascending, limit, ct),
					ttl
				) ?? [];
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GitHub API failed for user {User}. Attempting fallback cache...", username);
			
				if (_cache.TryGetValue(cacheKey, out List<GithubRepo>? cachedRepos))
					return cachedRepos;

				return [];
			}
		}

		private Task<List<GithubRepo>> FetchAndTrackAsync(
			string username,
			RepoSortMode? sort,
			bool ascending,
			int limit,
			CancellationToken ct)
		{
			return _tracker.TrackAsync(
				"GitHub",
				() => FetchFromGithubAsync(username, sort, ascending, limit, ct)
			);
		}

		private async Task<List<GithubRepo>> FetchFromGithubAsync(
			string username,
			RepoSortMode? sort,
			bool ascending,
			int limit,
			CancellationToken ct)
		{
			var url = $"https://api.github.com/users/{username}/repos?per_page={limit}";

			if (sort is RepoSortMode.Alphabetical or RepoSortMode.LastUpdated)
			{
				var sortValue = sort switch
				{
					RepoSortMode.Alphabetical => "full_name",
					RepoSortMode.LastUpdated => "updated",
					_ => null
				};

				if (sortValue != null)
				{
					url += $"&sort={sortValue}";
					url += $"&direction={(ascending ? "asc" : "desc")}";
				}
			}

			var response = await _http.GetAsync(url, ct);

			if (!response.IsSuccessStatusCode)
				return new List<GithubRepo>();

			using var json = await response.Content.ReadAsStreamAsync(ct);
			var repos = ParseRepos(json);

			if (sort == RepoSortMode.Stars)
			{
				repos = ascending
					? repos.OrderBy(r => r.Stars).ToList()
					: repos.OrderByDescending(r => r.Stars).ToList();
			}

			return repos;
		}

		private List<GithubRepo> ParseRepos(Stream jsonStream)
		{
			var doc = JsonDocument.Parse(jsonStream);
			var repos = new List<GithubRepo>();

			foreach (var repo in doc.RootElement.EnumerateArray())
			{
				repos.Add(new GithubRepo
				{
					Name = repo.GetProperty("name").GetString()!,
					HtmlUrl = repo.GetProperty("html_url").GetString()!,
					Stars = repo.GetProperty("stargazers_count").GetInt32(),
					LastUpdated = repo.GetProperty("updated_at").GetDateTime()
				});
			}

			return repos;
		}
	}
}
