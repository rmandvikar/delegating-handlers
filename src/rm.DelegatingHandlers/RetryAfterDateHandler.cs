using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Adds retry-after (date) header to response.
/// <para></para>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Retry-After">retry-after</see>
/// </summary>
public class RetryAfterDateHandler : DelegatingHandler
{
	private readonly DateTimeOffset date;

	/// <inheritdoc cref="RetryAfterDateHandler" />
	public RetryAfterDateHandler(
		DateTimeOffset date)
	{
		this.date = date;
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var response = await base.SendAsync(request, cancellationToken)
			.ConfigureAwait(false);

		var statusCode = (int)response.StatusCode;
		if (statusCode == 503
			|| statusCode == 429
			|| statusCode == 301)
		{
			response.Headers.Add(ResponseHeaders.RetryAfter, date.ToString("r"));
		}

		return response;
	}
}
