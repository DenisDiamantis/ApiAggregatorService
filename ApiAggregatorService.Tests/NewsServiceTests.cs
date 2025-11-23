using ApiAggregatorService.Models.Enums;
using ApiAggregatorService.Models.News;
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
	public class NewsServiceTests
	{
		[Fact]
		public async Task GetLatestHeadlinesAsync_ParsesArticles()
		{
			var json = @"{
                ""articles"": [
                    { ""title"": ""T1"", ""url"": ""http://a"" },
                    { ""title"": ""T2"", ""url"": ""http://b"" }
                ]
            }";

			var handler = new TestHandler((req, ct) =>
				new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
				});

			var client = new HttpClient(handler) { BaseAddress = new System.Uri("https://newsapi.org/") };
			var logger = new Mock<ILogger<NewsService>>();
			var factoryMock = new Mock<IHttpClientFactory>();
			factoryMock.Setup((f => f.CreateClient("News"))).Returns(client);

			var config = new Mock<IConfiguration>();
			config.Setup(c => c["NewsApi:BaseUrl"]).Returns("https://newsapi.org/");
			config.Setup(c => c["NewsApi:ApiKey"]).Returns("dummy");

			var stats = new Mock<IApiStatisticsService>();
			var tracker = new ApiPerformanceTracker(stats.Object);
			var cache = new ApiCacheService(new MemoryCache(new MemoryCacheOptions()));

			var svc = new NewsService(factoryMock.Object, config.Object, tracker, cache,logger.Object);
			var res = await svc.GetLatestHeadlinesAsync(NewsCategory.General);

			res.Should().NotBeNull();
			res.Articles.Should().HaveCountGreaterOrEqualTo(1);
			res.Articles[0].Title.Should().Be("T1");
		}

		[Fact]
		public async Task GetLatestHeadlinesAsync_WhenApiFails_ReturnsCachedValue()
		{
			var cache = new Mock<IApiCacheService>();
			var tracker = new FakeTracker();
			var logger = new Mock<ILogger<NewsService>>();

			var config = new Mock<IConfiguration>();
			config.Setup(c => c["NewsApi:BaseUrl"]).Returns("https://newsapi.org/");
			config.Setup(c => c["NewsApi:ApiKey"]).Returns("dummy");

			var factory = new Mock<IHttpClientFactory>();
			var http = new HttpClient(new FakeHttpHandler(HttpStatusCode.InternalServerError));
			factory.Setup(f => f.CreateClient("News")).Returns(http);

			var cached = new NewsResponse
			{
				Articles = new List<NewsArticle>
				{
					new NewsArticle { Title = "Cached" }
				}
			};

			cache.Setup(c => c.GetOrSetAsync(
				It.IsAny<string>(),
				It.IsAny<Func<Task<NewsResponse>>>(),
				It.IsAny<TimeSpan>()))
				.ThrowsAsync(new Exception("API failure"));

			cache.Setup(c => c.TryGetValue(It.IsAny<string>(), out cached))
				.Returns(true);

			var service = new NewsService(factory.Object, config.Object, tracker, cache.Object, logger.Object);

			var result = await service.GetLatestHeadlinesAsync(NewsCategory.General);

			Assert.Single(result.Articles);
			Assert.Equal("Cached", result.Articles[0].Title);
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
