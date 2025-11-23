using ApiAggregatorService.Models;
using ApiAggregatorService.Models.Enums;
using ApiAggregatorService.Services.Aggregation;
using ApiAggregatorService.Services.Cache;
using ApiAggregatorService.Services.External;
using ApiAggregatorService.Services.Statistics;
using ApiAggregatorService.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ApiAggregatorService.Tests
{
	public class GithubServiceTests
	{
		private HttpClient BuildHttpClient(string json, HttpStatusCode status = HttpStatusCode.OK)
		{
			var handler = new TestHandler((req, ct) =>
				new HttpResponseMessage(status)
				{
					Content = new StringContent(json ?? "[]", System.Text.Encoding.UTF8, "application/json")
				});

			return new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
		}

		private class TestHandler : HttpMessageHandler
		{
			private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _fn;
			public TestHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> fn) { _fn = fn; }
			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
				=> Task.FromResult(_fn(request, cancellationToken));
		}

		[Fact]
		public async Task GetUserReposAsync_ShouldParseRepos_AndUseCache()
		{
			var json = @"[
                { ""name"": ""a"", ""html_url"": ""u1"", ""stargazers_count"": 5, ""updated_at"": ""2024-01-01T00:00:00Z"" },
                { ""name"": ""b"", ""html_url"": ""u2"", ""stargazers_count"": 10, ""updated_at"": ""2024-02-01T00:00:00Z"" }
            ]";

			var loggerMock = new Mock<ILogger<GithubService>>();
			var client = BuildHttpClient(json);
			var factoryMock = new Mock<IHttpClientFactory>();
			factoryMock.Setup(f => f.CreateClient("GitHub")).Returns(client);

			var stats = new Mock<IApiStatisticsService>();
			var tracker = new ApiPerformanceTracker(stats.Object);

			var mem = new MemoryCache(new MemoryCacheOptions());
			var cache = new ApiCacheService(mem);

			var svc = new GithubService(factoryMock.Object, tracker, cache, loggerMock.Object);

			var repos = await svc.GetUserReposAsync("someuser");
			repos.Should().HaveCount(2);
			repos[0].Name.Should().Be("a");

			var repos2 = await svc.GetUserReposAsync("someuser");
			repos2.Should().HaveCount(2);

			stats.Verify(s => s.Record("GitHub", It.IsAny<double>()), Times.AtLeastOnce);
		}

		[Fact]
		public async Task GetUserReposAsync_ShouldApplyLocalStarsSort()
		{
			var json = @"[
                { ""name"": ""a"", ""html_url"": ""u1"", ""stargazers_count"": 1, ""updated_at"": ""2024-01-01T00:00:00Z"" },
                { ""name"": ""b"", ""html_url"": ""u2"", ""stargazers_count"": 10, ""updated_at"": ""2024-02-01T00:00:00Z"" }
            ]";

			var logger = new Mock<ILogger<GithubService>>();
			var client = BuildHttpClient(json);
			var factoryMock = new Mock<IHttpClientFactory>();
			factoryMock.Setup(f => f.CreateClient("GitHub")).Returns(client);

			var stats = new Mock<IApiStatisticsService>();
			var tracker = new ApiPerformanceTracker(stats.Object);

			var cache = new ApiCacheService(new MemoryCache(new MemoryCacheOptions()));
			var svc = new GithubService(factoryMock.Object, tracker, cache, logger.Object);

			var repos = await svc.GetUserReposAsync("u", RepoSortMode.Stars, ascending: false, limit: 10);

			repos.Should().BeInDescendingOrder(r => r.Stars);
		}

		[Fact]
		public async Task GetUserReposAsync_WhenApiFails_ReturnsCachedValue()
		{
			var cache = new Mock<IApiCacheService>();
			var tracker = new FakeTracker();
			var logger = new Mock<ILogger<GithubService>>();

			var factory = new Mock<IHttpClientFactory>();
			var http = new HttpClient(new FakeHttpHandler(HttpStatusCode.InternalServerError));
			factory.Setup(f => f.CreateClient("GitHub")).Returns(http);

			var cachedList = new List<GithubRepo>
			{
				new GithubRepo { Name = "cached" }
			};

			cache.Setup(c => c.GetOrSetAsync(
				It.IsAny<string>(),
				It.IsAny<Func<Task<List<GithubRepo>>>>(),
				It.IsAny<TimeSpan>()))
				.ThrowsAsync(new Exception("API failure"));

			cache.Setup(c => c.TryGetValue(It.IsAny<string>(), out cachedList))
				.Returns(true);

			var service = new GithubService(factory.Object, tracker, cache.Object, logger.Object);

			var result = await service.GetUserReposAsync("john");

			Assert.Single(result);
			Assert.Equal("cached", result[0].Name);
		}

	}
}