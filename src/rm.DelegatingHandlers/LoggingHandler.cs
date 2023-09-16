using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using rm.DelegatingHandlers.Formatting;
using Serilog;

namespace rm.DelegatingHandlers;

/// <summary>
/// Logs <see cref="HttpRequestMessage"/>, and <see cref="HttpResponseMessage"/> with
/// exception, if any, along with its elapsed time.
/// </summary>
public class LoggingHandler : DelegatingHandler
{
	private readonly ILogger logger;
	private readonly ILoggingFormatter loggingFormatter;

	/// <inheritdoc cref="LoggingHandler" />
	public LoggingHandler(
		ILogger logger,
		ILoggingFormatter loggingFormatter)
	{
		this.logger = logger?.ForContext(GetType())
			?? throw new ArgumentNullException(nameof(logger));
		this.loggingFormatter = loggingFormatter
			?? throw new ArgumentNullException(nameof(loggingFormatter));
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		ILogger lRequest = logger;
		lRequest = await lRequest.ForContextAsync(request, loggingFormatter)
			.ConfigureAwait(false);
		lRequest.Information("request/");

		var stopwatch = Stopwatch.StartNew();
		try
		{
			var response = await base.SendAsync(request, cancellationToken)
				.ConfigureAwait(false);

			stopwatch.Stop();

			ILogger lResponse = logger;
			lResponse = await lResponse.ForContextAsync(request, loggingFormatter)
				.ConfigureAwait(false);
			lResponse = await lResponse.ForContextAsync(response, loggingFormatter)
				.ConfigureAwait(false);
			lResponse = lResponse.ForContext(stopwatch, loggingFormatter);
			lResponse.Information("request/response");

			return response;
		}
		catch (Exception ex)
		{
			stopwatch.Stop();

			ILogger lException = logger;
			lException = await logger.ForContextAsync(request, loggingFormatter)
				.ConfigureAwait(false);
			lException = lException.ForContext(ex, loggingFormatter);
			lException = lException.ForContext(stopwatch, loggingFormatter);
			lException.Information(ex, "request/exception");

			throw;
		}
	}
}
