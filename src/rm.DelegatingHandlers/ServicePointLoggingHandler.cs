using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Core.Enrichers;

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
		var response = await base.SendAsync(request, cancellationToken)
			.ConfigureAwait(false);

		var servicePoint = ServicePointManager.FindServicePoint(request.RequestUri);

		var props =
			new ILogEventEnricher[]
			{
				// global
				new PropertyEnricher("CheckCertificateRevocationList", ServicePointManager.CheckCertificateRevocationList),
				new PropertyEnricher("DefaultConnectionLimit", ServicePointManager.DefaultConnectionLimit),
				new PropertyEnricher("DefaultNonPersistentConnectionLimit", ServicePointManager.DefaultNonPersistentConnectionLimit),
				new PropertyEnricher("DefaultPersistentConnectionLimit", ServicePointManager.DefaultPersistentConnectionLimit),
				new PropertyEnricher("DnsRefreshTimeout", ServicePointManager.DnsRefreshTimeout),
				new PropertyEnricher("EnableDnsRoundRobin", ServicePointManager.EnableDnsRoundRobin),
				new PropertyEnricher("EncryptionPolicy", ServicePointManager.EncryptionPolicy),
				new PropertyEnricher("Expect100Continue", ServicePointManager.Expect100Continue),
				new PropertyEnricher("MaxServicePointIdleTime", ServicePointManager.MaxServicePointIdleTime),
				new PropertyEnricher("MaxServicePoints", ServicePointManager.MaxServicePoints),
				new PropertyEnricher("ReusePort", ServicePointManager.ReusePort),
				new PropertyEnricher("SecurityProtocol", ServicePointManager.SecurityProtocol),
				new PropertyEnricher("UseNagleAlgorithm", ServicePointManager.UseNagleAlgorithm),

				// servicePoint
				new PropertyEnricher("host.Key", $"{servicePoint.Address.Scheme}://{servicePoint.Address.Host}"),
				new PropertyEnricher("host.Address", servicePoint.Address),
				new PropertyEnricher("host.ConnectionName", servicePoint.ConnectionName),
				new PropertyEnricher("host.ProtocolVersion", servicePoint.ProtocolVersion),
				new PropertyEnricher("host.Expect100Continue", servicePoint.Expect100Continue),
				new PropertyEnricher("host.UseNagleAlgorithm", servicePoint.UseNagleAlgorithm),
				new PropertyEnricher("host.SupportsPipelining", servicePoint.SupportsPipelining),
				new PropertyEnricher("host.ConnectionLimit", servicePoint.ConnectionLimit),
				new PropertyEnricher("host.CurrentConnections", servicePoint.CurrentConnections),
				new PropertyEnricher("host.ConnectionLeaseTimeout", servicePoint.ConnectionLeaseTimeout),
				new PropertyEnricher("host.IdleSince", servicePoint.IdleSince),
				new PropertyEnricher("host.MaxIdleTime", servicePoint.MaxIdleTime),
				new PropertyEnricher("host.ReceiveBufferSize", servicePoint.ReceiveBufferSize),
			};

		logger.ForContext(props)
			.Information("ServicePoint stats");

		return response;
	}
}
