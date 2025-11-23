using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using ApiAggregatorService.Services.Cache;

namespace ApiAggregatorService.Tests
{
	public class ApiCacheServiceTests
	{
		[Fact]
		public async Task GetOrSetAsync_ShouldStoreAndReturnCachedValue()
		{
			var memoryCache = new MemoryCache(new MemoryCacheOptions());
			var cache = new ApiCacheService(memoryCache);

			var calls = 0;
			var res1 = await cache.GetOrSetAsync("key1", async () =>
			{
				await Task.Delay(1);
				calls++;
				return "value1";
			}, TimeSpan.FromMinutes(1));

			var res2 = await cache.GetOrSetAsync("key1", () => Task.FromResult("value2"), TimeSpan.FromMinutes(1));

			res1.Should().Be("value1");
			res2.Should().Be("value1");
			calls.Should().Be(1);
		}
	}
}
