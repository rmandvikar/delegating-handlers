using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Causes delay after N requests to induce http timeouts.
/// </summary>
public class ProcrastinatingAfterNRequestsHandler : DelegatingHandler
{
	private readonly IProcrastinatingAfterNRequestsHandlerSettings procrastinatingAfterNRequestsHandlerSettings;

	private long n;

	/// <inheritdoc cref="ProcrastinatingAfterNRequestsHandler" />
	public ProcrastinatingAfterNRequestsHandler(
		IProcrastinatingAfterNRequestsHandlerSettings procrastinatingAfterNRequestsHandlerSettings)
	{
		this.procrastinatingAfterNRequestsHandlerSettings = procrastinatingAfterNRequestsHandlerSettings
			?? throw new ArgumentNullException(nameof(procrastinatingAfterNRequestsHandlerSettings));
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		if (Interlocked.Read(ref n) >= procrastinatingAfterNRequestsHandlerSettings.N)
		{
			await Task.Delay(procrastinatingAfterNRequestsHandlerSettings.DelayInMilliseconds, cancellationToken)
				.ConfigureAwait(false);
		}

		var response = await base.SendAsync(request, cancellationToken)
			.ConfigureAwait(false);

		if (Interlocked.Read(ref n) < procrastinatingAfterNRequestsHandlerSettings.N)
		{
			Interlocked.Increment(ref n);
		}

		return response;
	}
}

public interface IProcrastinatingAfterNRequestsHandlerSettings
{
	long N { get; }
	int DelayInMilliseconds { get; }
}

public record class ProcrastinatingAfterNRequestsHandlerSettings : IProcrastinatingAfterNRequestsHandlerSettings
{
	public long N { get; init; }
	public int DelayInMilliseconds { get; init; }
}
