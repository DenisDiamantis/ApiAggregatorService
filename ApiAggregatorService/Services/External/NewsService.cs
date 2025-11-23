using ApiAggregatorService.Models.Enums;
using ApiAggregatorService.Models.News;
using ApiAggregatorService.Services.Cache;
using ApiAggregatorService.Services.Statistics;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ApiAggregatorService.Services.External
{
	public class NewsService : INewsService
	{
		private readonly HttpClient _http;
		private readonly string _apiKey;
		private readonly ApiPerformanceTracker _tracker;
		private readonly IApiCacheService _cache;
		private readonly ILogger<NewsService> _logger;

		public NewsService(
			IHttpClientFactory factory,
			IConfiguration config,
			ApiPerformanceTracker tracker,
			IApiCacheService cache,
			ILogger<NewsService> logger)
		{
			_http = factory.CreateClient("News");
			_http.BaseAddress = new Uri(config["NewsApi:BaseUrl"]);
			_http.DefaultRequestHeaders.UserAgent.ParseAdd("ApiAggregatorService/1.0");

			_apiKey = config["NewsApi:ApiKey"];
			_tracker = tracker;
			_cache = cache;
			_logger = logger;
		}

		public async Task<NewsResponse> GetLatestHeadlinesAsync(NewsCategory category)
		{
			var cacheKey = $"news_headlines_{category}";
			var ttl = TimeSpan.FromMinutes(2);

			try
			{
				return await _cache.GetOrSetAsync(
					cacheKey,
					() => FetchAndTrackAsync(category),
					ttl
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "News API failed for category {Category}. Attempting fallback cache...", category);
				if (_cache.TryGetValue(cacheKey, out NewsResponse? cached))
					return cached;

				return new NewsResponse();
			}
		}

		private Task<NewsResponse> FetchAndTrackAsync(NewsCategory category)
		{
			return _tracker.TrackAsync("News", () => FetchFromApiAsync(category));
		}

		private async Task<NewsResponse> FetchFromApiAsync(NewsCategory category)
		{
			var url = $"top-headlines?category={category.ToString().ToLower()}&apiKey={_apiKey}";
			var response = await _http.GetAsync(url);

			if (!response.IsSuccessStatusCode)
				return new NewsResponse();

			using var jsonStream = await response.Content.ReadAsStreamAsync();
			return ParseNews(jsonStream);
		}

		private NewsResponse ParseNews(Stream jsonStream)
		{
			var document = JsonDocument.Parse(jsonStream);

			var articles = document.RootElement
				.GetProperty("articles")
				.EnumerateArray()
				.Take(5)
				.Select(a => new NewsArticle
				{
					Title = a.GetProperty("title").GetString(),
					Url = a.GetProperty("url").GetString()
				})
				.ToList();

			return new NewsResponse { Articles = articles };
		}
	}
}
