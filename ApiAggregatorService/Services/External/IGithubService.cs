using ApiAggregatorService.Models;
using ApiAggregatorService.Models.Enums;

namespace ApiAggregatorService.Services.External
{
	public interface IGithubService
	{
		Task<List<GithubRepo>> GetUserReposAsync(
			string username,
			RepoSortMode? sort = null,
			bool ascending = false,
			int limit = 3,
			CancellationToken ct = default);
	}
}
