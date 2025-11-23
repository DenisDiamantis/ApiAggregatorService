using ApiAggregatorService.Models.Enums;
using ApiAggregatorService.Models.News;

namespace ApiAggregatorService.Services.External
{
	public interface INewsService
	{
		Task<NewsResponse> GetLatestHeadlinesAsync(NewsCategory category);
	}

}
