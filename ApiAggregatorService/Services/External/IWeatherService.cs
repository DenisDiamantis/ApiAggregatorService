using ApiAggregatorService.Models;

namespace ApiAggregatorService.Services.External
{
	public interface IWeatherService
	{
		Task<WeatherResponse?> GetWeatherAsync(string city);
	}

}
