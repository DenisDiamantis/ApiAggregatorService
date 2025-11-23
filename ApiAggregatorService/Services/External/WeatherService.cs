using ApiAggregatorService.Models;
using ApiAggregatorService.Services.Cache;
using ApiAggregatorService.Services.Statistics;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ApiAggregatorService.Services.External
{
	public class WeatherService : IWeatherService
	{
		private readonly HttpClient _http;
		private readonly string _apiKey;
		private readonly ApiPerformanceTracker _tracker;
		private readonly IApiCacheService _cache;
		private readonly ILogger<WeatherService> _logger;

		public WeatherService(
			IHttpClientFactory factory,
			IConfiguration config,
			ApiPerformanceTracker tracker,
			IApiCacheService cache,
			ILogger<WeatherService> logger)
		{
			_http = factory.CreateClient("Weather");
			_http.BaseAddress = new Uri(config["WeatherApi:BaseUrl"]);

			_apiKey = config["WeatherApi:ApiKey"];
			_tracker = tracker;
			_cache = cache;
			_logger = logger;
		}

		public async Task<WeatherResponse?> GetWeatherAsync(string city)
		{
			var cacheKey = $"weather_{city.ToLower()}";
			var ttl = TimeSpan.FromMinutes(5);

			try
			{
				return await _cache.GetOrSetAsync(
					cacheKey,
					() => FetchAndTrackAsync(city),
					ttl
				);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Weather API failed — returning cached fallback for {City}", city);

				if (_cache.TryGetValue(cacheKey, out WeatherResponse? fallback))
					return fallback;

				return null;
			}
		}

		private Task<WeatherResponse?> FetchAndTrackAsync(string city)
		{
			return _tracker.TrackAsync("OpenWeather", () => FetchFromApiAsync(city));
		}

		private async Task<WeatherResponse?> FetchFromApiAsync(string city)
		{
			var url = $"weather?q={city}&appid={_apiKey}&units=metric";

			var response = await _http.GetAsync(url);

			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning(
					"Weather API returned {StatusCode} for {City}",
					response.StatusCode, city);

				return null;
			}

			using var jsonStream = await response.Content.ReadAsStreamAsync();
			return ParseWeather(jsonStream, city);
		}

		private WeatherResponse ParseWeather(Stream stream, string city)
		{
			var doc = JsonDocument.Parse(stream);

			return new WeatherResponse
			{
				City = city,
				TemperatureC = doc.RootElement.GetProperty("main").GetProperty("temp").GetDouble(),
				Summary = doc.RootElement.GetProperty("weather")[0].GetProperty("description").GetString()
			};
		}
	}
}
