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
		ILogger l = logger;
		l = await l.ForContextAsync(request, loggingFormatter)
			.ConfigureAwait(false);
		l.Information("request/");

		var stopwatch = Stopwatch.StartNew();
		try
		{
			var response = await base.SendAsync(request, cancellationToken)
				.ConfigureAwait(false);

			stopwatch.Stop();

			l = await l.ForContextAsync(response, loggingFormatter)
				.ConfigureAwait(false);
			l = l.ForContext(stopwatch, loggingFormatter);
			l.Information("request/response");

			return response;
		}
		catch (Exception ex)
		{
			stopwatch.Stop();

			l = l.ForContext(ex, loggingFormatter);
			l = l.ForContext(stopwatch, loggingFormatter);
			l.Information(ex, "request/exception");

			throw;
		}
	}
}
