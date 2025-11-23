using ApiAggregatorService.Models;
using ApiAggregatorService.Models.Enums;

namespace ApiAggregatorService.Services.Aggregation
{
	public interface IAggregatorService
	{
		/// <summary>
		/// Aggregate data from all external APIs.
		/// </summary>
		/// <param name="cityWeather">City for weather</param>
		/// <param name="category">Category for news</param>
		/// <param name="githubUser">GitHub username</param>
		/// <param name="sortBy">optional: date | relevance | stars</param>
		/// <param name="ascending">sort order</param>
		/// <param name="limit">maximum items per section (0 => default)</param>
		Task<AggregatedResponse> AggregateAsync(
			string cityWeather,
			NewsCategory category,
			string githubUser,
			RepoSortMode? repoSort,
			bool ascending = false,
			int limit = 3,
			CancellationToken ct = default);
	}

}
