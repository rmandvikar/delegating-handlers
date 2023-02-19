using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Adds retry-after (delay) header to response.
/// <para></para>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Retry-After">retry-after</see>
/// </summary>
public class RetryAfterDelayHandler : DelegatingHandler
{
	private readonly double delayInSeconds;

	/// <inheritdoc cref="RetryAfterDelayHandler" />
	public RetryAfterDelayHandler(
		double delayInSeconds)
	{
		this.delayInSeconds = delayInSeconds;
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
			response.Headers.Add(ResponseHeaders.RetryAfter, delayInSeconds.ToString());
		}

		return response;
	}
}
