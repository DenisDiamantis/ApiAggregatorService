namespace ApiAggregatorService.Models
{
	public class GithubRepo
	{
		public string Name { get; set; }
		public string HtmlUrl { get; set; }
		public int Stars { get; set; }
		public DateTime LastUpdated { get; set; }
	}
}
