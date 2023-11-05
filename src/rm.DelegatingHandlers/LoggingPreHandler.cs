using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using rm.DelegatingHandlers.Formatting;
using Serilog;

namespace rm.DelegatingHandlers;

/// <summary>
/// Logs <see cref="HttpRequestMessage"/>.
/// </summary>
public class LoggingPreHandler : DelegatingHandler
{
	private readonly ILogger logger;
	private readonly ILoggingFormatter loggingFormatter;

	/// <inheritdoc cref="LoggingPreHandler" />
	public LoggingPreHandler(
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

		return await base.SendAsync(request, cancellationToken)
			.ConfigureAwait(false);
	}
}
