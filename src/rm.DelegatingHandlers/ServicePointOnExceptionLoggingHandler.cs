using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace rm.DelegatingHandlers;

/// <summary>
/// Logs <see cref="ServicePoint"/> stats for <see cref="HttpRequestMessage.RequestUri"/> with exception.
/// </summary>
public class ServicePointOnExceptionLoggingHandler : DelegatingHandler
{
	private readonly ILogger logger;

	/// <inheritdoc cref="ServicePointOnExceptionLoggingHandler" />
	public ServicePointOnExceptionLoggingHandler(
		ILogger logger)
	{
		this.logger = logger?.ForContext(GetType())
			?? throw new ArgumentNullException(nameof(logger));
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		try
		{
			return await base.SendAsync(request, cancellationToken)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			var servicePoint = ServicePointManager.FindServicePoint(request.RequestUri);
			var props = ServicePointHelpers.GetServicePointEnrichers(servicePoint);

			logger.ForContext(props)
				.Error(ex, "ServicePoint stats");

			throw;
		}
	}
}
