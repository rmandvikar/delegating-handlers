using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Short-circuits with canned actions.
/// </summary>
/// <remarks>
/// Canned action's response could be disposed as it's not meant for multiuse.
/// </remarks>
public class ShortCircuitingCannedActionsHandler : DelegatingHandler, IDisposable
{
	private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>[] actions;
	private int iActions = 0;

	private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

	/// <inheritdoc cref="ShortCircuitingCannedActionsHandler" />
	public ShortCircuitingCannedActionsHandler(
		params Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>[] actions)
	{
		this.actions = actions
			?? throw new ArgumentNullException(nameof(actions));
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		await semaphoreSlim.WaitAsync();
		try
		{
			if (iActions < actions.Length)
			{
				return await actions[iActions++](request, cancellationToken)
					.ConfigureAwait(false);
			}
		}
		finally
		{
			semaphoreSlim.Release();
		}
		return await base.SendAsync(request, cancellationToken)
			.ConfigureAwait(false);
	}

	private bool disposed = false;

	protected override void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				semaphoreSlim?.Dispose();

				disposed = true;
			}
		}

		base.Dispose(disposing);
	}
}
