using System.Diagnostics;

namespace ApiAggregatorService.Middleware
{
	public class LoggingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<LoggingMiddleware> _logger;

		public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			var sw = Stopwatch.StartNew();
			var request = context.Request;

			_logger.LogInformation(
				"Incoming request {Method} {Path}",
				request.Method,
				request.Path
			);

			try
			{
				await _next(context);
			}
			catch (Exception ex)
			{
				_logger.LogError(
					ex,
					"Exception caught while processing {Method} {Path}",
					request.Method,
					request.Path
				);
				throw; 
			}
			finally
			{
				sw.Stop();

				_logger.LogInformation(
					"Response {StatusCode} for {Method} {Path} in {Duration} ms",
					context.Response.StatusCode,
					request.Method,
					request.Path,
					sw.ElapsedMilliseconds
				);
			}
		}
	}
}
