using ApiAggregatorService.Models;
using ApiAggregatorService.Models.Enums;
using ApiAggregatorService.Models.News;
using ApiAggregatorService.Services.Aggregation;
using ApiAggregatorService.Services.External;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ApiAggregatorService.Tests
{
	public class AggregatorServiceTests
	{
		[Fact]
		public async Task AggregateAsync_ReturnsCombinedResponse_WithSortingAndLimit()
		{
			var weatherMock = new Mock<IWeatherService>();
			var githubMock = new Mock<IGithubService>();
			var newsMock = new Mock<INewsService>();
			var loggerMock = new Mock<ILogger<AggregatorService>>();

			weatherMock.Setup(w => w.GetWeatherAsync(It.IsAny<string>()))
					   .ReturnsAsync(new WeatherResponse { City = "Paris", TemperatureC = 20, Summary = "Sunny" });

			newsMock.Setup(n => n.GetLatestHeadlinesAsync(It.IsAny<NewsCategory>()))
					.ReturnsAsync(new NewsResponse
					{
						Articles = new List<NewsArticle>
						{
					new NewsArticle { Title = "A1", Url = "url1" },
					new NewsArticle { Title = "A2", Url = "url2" }
						}
					});

			githubMock.Setup(g => g.GetUserReposAsync(
								It.IsAny<string>(),
								It.IsAny<RepoSortMode?>(),
								It.IsAny<bool>(),
								It.IsAny<int>(),
								It.IsAny<CancellationToken>()))
					  .ReturnsAsync(new List<GithubRepo>
					  {
				  new GithubRepo { Name = "Zeta", Stars = 100 },
				  new GithubRepo { Name = "Alpha", Stars = 200 }
					  });

			var service = new AggregatorService(
				weatherMock.Object, githubMock.Object, newsMock.Object, loggerMock.Object
				);

			var result = await service.AggregateAsync(
				"Paris",
				NewsCategory.Business,
				"john",
				RepoSortMode.Stars,
				ascending: false,
				limit: 1
			);

			result.Should().NotBeNull();
			result.GithubRepos.Should().HaveCount(1);
			result.GithubRepos.First().Name.Should().Be("Alpha");
		}

	}
}
