using Microsoft.Extensions.Caching.Memory;

namespace ApiAggregatorService.Services.Cache
{
	public class ApiCacheService: IApiCacheService
	{
		private readonly IMemoryCache _cache;

		public ApiCacheService(IMemoryCache cache)
		{
			_cache = cache;
		}

		public async Task<T?> GetOrSetAsync<T>(
			string key,
			Func<Task<T?>> factory,
			TimeSpan ttl)
		{
			if (_cache.TryGetValue(key, out T? value))
				return value;

			var result = await factory();

			if (result is not null)
				_cache.Set(key, result, ttl);

			return result;
		}

		public bool TryGetValue<T>(string key, out T? value)
		{
			if (_cache.TryGetValue(key, out var raw) && raw is T typed)
			{
				value = typed;
				return true;
			}

			value = default;
			return false;
		}
	}
}
