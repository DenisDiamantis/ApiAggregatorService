using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApiAggregatorService.Tests.TestHelpers
{
	public class FakeHttpHandler : HttpMessageHandler
	{
		private readonly HttpStatusCode _statusCode;
		private readonly string _content;

		public FakeHttpHandler(HttpStatusCode statusCode, string content = "")
		{
			_statusCode = statusCode;
			_content = content;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var response = new HttpResponseMessage(_statusCode)
			{
				Content = new StringContent(_content, Encoding.UTF8, "application/json")
			};

			return Task.FromResult(response);
		}
	}

}
