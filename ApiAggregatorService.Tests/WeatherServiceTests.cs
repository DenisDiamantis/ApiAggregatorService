using ApiAggregatorService.Models;
using ApiAggregatorService.Services.Cache;
using ApiAggregatorService.Services.External;
using ApiAggregatorService.Services.Statistics;
using ApiAggregatorService.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace ApiAggregatorService.Tests
{
	public class WeatherServiceTests
	{
		[Fact]
		public async Task GetWeatherAsync_ShouldReturnParsedWeather()
		{
			var json = @"{
                ""main"": { ""temp"": 12.5 },
                ""weather"": [ { ""description"": ""sunny"" } ]
            }";

			var handler = new TestHandler((req, ct) =>
				new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
				});

			var client = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.openweathermap.org/") };
			var logger = new Mock<ILogger<WeatherService>>();
			var factoryMock = new Mock<IHttpClientFactory>();
			factoryMock.Setup((f => f.CreateClient("Weather"))).Returns(client);

			var stats = new Mock<IApiStatisticsService>();
			var tracker = new ApiPerformanceTracker(stats.Object);

			var cache = new ApiCacheService(new MemoryCache(new MemoryCacheOptions()));

			var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
			config.Setup(c => c["WeatherApi:BaseUrl"]).Returns("https://api.openweathermap.org/");
			config.Setup(c => c["WeatherApi:ApiKey"]).Returns("dummy");

			var svc = new WeatherService(factoryMock.Object, config.Object, tracker, cache, logger.Object);
			var w = await svc.GetWeatherAsync("London");

			w.Should().NotBeNull();
			w.City.Should().Be("London");
			w.TemperatureC.Should().Be(12.5);
			w.Summary.Should().Be("sunny");
		}


		[Fact]
		public async Task GetWeatherAsync_WhenApiFails_ReturnsCachedValue()
		{
			var cache = new Mock<IApiCacheService>();
			var tracker = new FakeTracker();
			var logger = new Mock<ILogger<WeatherService>>();

			var city = "London";

			var factory = new Mock<IHttpClientFactory>();
			var http = new HttpClient(new FakeHttpHandler(HttpStatusCode.InternalServerError));
			factory.Setup(f => f.CreateClient("Weather")).Returns(http);

			var cached = new WeatherResponse
			{
				City = city,
				TemperatureC = 20,
				Summary = "Cached sunny"
			};

			cache.Setup(c => c.GetOrSetAsync(
					It.IsAny<string>(),
					It.IsAny<Func<Task<WeatherResponse>>>(),
					It.IsAny<TimeSpan>()))
				.ThrowsAsync(new Exception("API failure"));

			cache.Setup(c => c.TryGetValue(It.IsAny<string>(), out cached))
				.Returns(true);

			var config = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string>
				{
					["WeatherApi:BaseUrl"] = "https://api.openweathermap.org/data/2.5/",
					["WeatherApi:ApiKey"] = "test"
				})
				.Build();

			var service = new WeatherService(
				factory.Object,
				config,
				tracker,
				cache.Object,
				logger.Object
			);

			var result = await service.GetWeatherAsync(city);

			Assert.NotNull(result);
			Assert.Equal("London", result.City);
			Assert.Equal(20, result.TemperatureC);
			Assert.Equal("Cached sunny", result.Summary);
		}

		private class TestHandler : HttpMessageHandler
		{
			private readonly System.Func<HttpRequestMessage, System.Threading.CancellationToken, HttpResponseMessage> _fn;
			public TestHandler(System.Func<HttpRequestMessage, System.Threading.CancellationToken, HttpResponseMessage> fn) => _fn = fn;
			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
				=> Task.FromResult(_fn(request, cancellationToken));
		}
	}
}
