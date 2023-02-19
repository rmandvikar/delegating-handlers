using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Adds header to response.
/// </summary>
public class ResponseHeaderHandler : DelegatingHandler
{
	private readonly string headerName;
	private readonly string headerValue;
	private readonly HttpHeaderTarget httpHeaderTarget;

	/// <inheritdoc cref="ResponseHeaderHandler" />
	public ResponseHeaderHandler(
		string headerName, string headerValue, HttpHeaderTarget httpHeaderTarget)
	{
		this.headerName = headerName
			?? throw new ArgumentNullException(nameof(headerName));
		this.headerValue = headerValue
			?? throw new ArgumentNullException(nameof(headerValue));
		this.httpHeaderTarget = httpHeaderTarget;
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var response = await base.SendAsync(request, cancellationToken);

		if (httpHeaderTarget == HttpHeaderTarget.Message)
		{
			response.Headers.Add(headerName, headerValue);
		}
		else if (httpHeaderTarget == HttpHeaderTarget.MessageContent)
		{
			response.Content.Headers.Add(headerName, headerValue);
		}

		return response;
	}
}
