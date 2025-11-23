using FluentAssertions;
using Xunit;
using ApiAggregatorService.Services.Statistics;

namespace ApiAggregatorService.Tests
{
	public class ApiStatisticsServiceTests
	{
		[Fact]
		public void Record_ShouldPopulateBucketsAndCounts()
		{
			var stats = new ApiStatisticsService();

			stats.Record("X", 50);   
			stats.Record("X", 250);  
			stats.Record("X", 600); 

			var response = stats.GetStatistics();
			response.ApiStats.Should().ContainKey("X");
			var s = response.ApiStats["X"];

			s.TotalRequests.Should().Be(3);
			s.FastCount.Should().Be(1);
			s.MediumCount.Should().Be(1);
			s.SlowCount.Should().Be(1);
			s.TotalDurationMs.Should().BeGreaterThan(0);
		}
	}
}
