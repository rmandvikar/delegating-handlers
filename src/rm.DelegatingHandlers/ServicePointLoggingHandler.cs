using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace rm.DelegatingHandlers;

/// <summary>
/// Logs <see cref="ServicePoint"/> stats for <see cref="HttpRequestMessage.RequestUri"/>.
/// </summary>
public class ServicePointLoggingHandler : DelegatingHandler
{
	private readonly ILogger logger;

	/// <inheritdoc cref="ServicePointLoggingHandler" />
	public ServicePointLoggingHandler(
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
		finally
		{
			var servicePoint = ServicePointManager.FindServicePoint(request.RequestUri);
			var props = ServicePointHelpers.GetServicePointEnrichers(servicePoint);

			logger.ForContext(props)
				.Information("ServicePoint stats");
		}
	}
}
