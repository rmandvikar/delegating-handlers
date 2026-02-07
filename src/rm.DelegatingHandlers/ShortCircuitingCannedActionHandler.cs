using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Short-circuits with canned action.
/// </summary>
/// <remarks>
/// Canned action's response could be disposed as it's not meant for multiuse.
/// </remarks>
public class ShortCircuitingCannedActionHandler : DelegatingHandler
{
	private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action;

	/// <inheritdoc cref="ShortCircuitingCannedActionHandler" />
	public ShortCircuitingCannedActionHandler(
		Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action)
	{
		this.action = action
			?? throw new ArgumentNullException(nameof(action));
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		return await action(request, cancellationToken)
			.ConfigureAwait(false);
	}
}
