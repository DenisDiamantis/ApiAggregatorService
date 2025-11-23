namespace ApiAggregatorService.Services.Cache
{
	public interface IApiCacheService
	{
		Task<T> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan ttl);
		bool TryGetValue<T>(string key, out T? value);
	}

}
