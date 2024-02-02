using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Runs delegates after N requests.
/// </summary>
public class DelegateAfterNRequestsHandler : DelegatingHandler
{
	private readonly IDelegateAfterNRequestsHandlerSettings delegateAfterNRequestsHandlerSettings;

	private long n;

	/// <inheritdoc cref="DelegateAfterNRequestsHandler" />
	public DelegateAfterNRequestsHandler(
		IDelegateAfterNRequestsHandlerSettings delegateAfterNRequestsHandlerSettings)
	{
		this.delegateAfterNRequestsHandlerSettings = delegateAfterNRequestsHandlerSettings
			?? throw new ArgumentNullException(nameof(delegateAfterNRequestsHandlerSettings));
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var nValue = Interlocked.Read(ref n);
		var thresholdMet = nValue >= delegateAfterNRequestsHandlerSettings.N;
		if (thresholdMet)
		{
			await delegateAfterNRequestsHandlerSettings.PreDelegate(request)
				.ConfigureAwait(false);
		}

		var response = await base.SendAsync(request, cancellationToken)
			.ConfigureAwait(false);

		if (thresholdMet)
		{
			await delegateAfterNRequestsHandlerSettings.PostDelegate(request, response)
				.ConfigureAwait(false);
		}

		if (nValue < delegateAfterNRequestsHandlerSettings.N)
		{
			Interlocked.Increment(ref n);
		}

		return response;
	}
}

public interface IDelegateAfterNRequestsHandlerSettings
{
	long N { get; }
	Func<HttpRequestMessage, Task> PreDelegate { get; }
	Func<HttpRequestMessage, HttpResponseMessage, Task> PostDelegate { get; }
}

public record class DelegateAfterNRequestsHandlerSettings : IDelegateAfterNRequestsHandlerSettings
{
	public long N { get; init; }
	public Func<HttpRequestMessage, Task> PreDelegate { get; init; }
	public Func<HttpRequestMessage, HttpResponseMessage, Task> PostDelegate { get; init; }
}
