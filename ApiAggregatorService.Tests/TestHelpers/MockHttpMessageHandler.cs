using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ApiAggregatorService.Tests.TestHelpers
{
	public class MockHttpMessageHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _responder;

		public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
		{
			_responder = responder ?? throw new ArgumentNullException(nameof(responder));
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return Task.FromResult(_responder(request, cancellationToken));
		}

		public static HttpClient CreateClientReturning(string content, string mediaType = "application/json", HttpStatusCode status = HttpStatusCode.OK)
		{
			var handler = new MockHttpMessageHandler((req, ct) =>
				new HttpResponseMessage(status)
				{
					Content = new StringContent(content ?? string.Empty, System.Text.Encoding.UTF8, mediaType)
				});

			return new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
		}
	}
}
