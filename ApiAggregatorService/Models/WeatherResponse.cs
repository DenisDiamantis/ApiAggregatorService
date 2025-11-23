namespace ApiAggregatorService.Models
{
	public class WeatherResponse
	{
		public string City { get; set; }
		public double TemperatureC { get; set; }
		public double TemperatureF => TemperatureC * 9 / 5 + 32;
		public string Summary { get; set; }
	}

}
