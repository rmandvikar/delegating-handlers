using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Uses http2.
/// </summary>
public class Http2Handler : DelegatingHandler
{
	/// <inheritdoc cref="Http2Handler" />
	public Http2Handler()
	{ }

	protected override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request, CancellationToken cancellationToken)
	{
		// HttpVersion.Version20 is not available in net framework
		request.Version = new Version("2.0");
		return base.SendAsync(request, cancellationToken);
	}
}
