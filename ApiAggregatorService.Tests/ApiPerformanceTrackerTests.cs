using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ApiAggregatorService.Services.Statistics;
using Xunit;

namespace ApiAggregatorService.Tests
{
	public class ApiPerformanceTrackerTests
	{
		[Fact]
		public async Task TrackAsync_ShouldCallStatsRecord()
		{
			var mockStats = new Mock<IApiStatisticsService>();
			var tracker = new ApiPerformanceTracker(mockStats.Object);

			var result = await tracker.TrackAsync("X", async () =>
			{
				await Task.Delay(5);
				return 42;
			});

			result.Should().Be(42);
			mockStats.Verify(s => s.Record(It.Is<string>(x => x == "X"), It.IsAny<double>()), Times.Once);
		}

		[Fact]
		public async Task TrackAsync_ShouldRecordEvenIfActionThrows()
		{
			var mockStats = new Mock<IApiStatisticsService>();
			var tracker = new ApiPerformanceTracker(mockStats.Object);

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				tracker.TrackAsync<int>("Err", () => throw new InvalidOperationException("fail"))
			);

			mockStats.Verify(s => s.Record("Err", It.IsAny<double>()), Times.Once);
		}
	}
}
