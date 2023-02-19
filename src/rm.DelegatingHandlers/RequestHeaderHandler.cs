using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Adds header to request.
/// </summary>
public class RequestHeaderHandler : DelegatingHandler
{
	private readonly string headerName;
	private readonly string headerValue;
	private readonly HttpHeaderTarget httpHeaderTarget;

	/// <inheritdoc cref="RequestHeaderHandler" />
	public RequestHeaderHandler(
		string headerName, string headerValue, HttpHeaderTarget httpHeaderTarget)
	{
		this.headerName = headerName
			?? throw new ArgumentNullException(nameof(headerName));
		this.headerValue = headerValue
			?? throw new ArgumentNullException(nameof(headerValue));
		this.httpHeaderTarget = httpHeaderTarget;
	}

	protected override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		if (httpHeaderTarget == HttpHeaderTarget.Message)
		{
			request.Headers.Add(headerName, headerValue);
		}
		else if (httpHeaderTarget == HttpHeaderTarget.MessageContent)
		{
			request.Content.Headers.Add(headerName, headerValue);
		}

		return base.SendAsync(request, cancellationToken);
	}
}
