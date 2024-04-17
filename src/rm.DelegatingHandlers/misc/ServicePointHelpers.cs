using System.Net;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace rm.DelegatingHandlers;

public static class ServicePointHelpers
{
	public static ILogEventEnricher[] GetServicePointEnrichers(ServicePoint servicePoint)
	{
		return
			new[]
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
				// see https://stackoverflow.com/questions/75168666/should-i-pass-the-full-url-or-just-the-domain-to-servicepointmanager-findservice
				new PropertyEnricher("host.Key", $"{servicePoint.Address.Scheme}://{servicePoint.Address.DnsSafeHost}"),
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
	}
}
