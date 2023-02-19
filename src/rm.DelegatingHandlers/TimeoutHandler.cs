using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Times out after given timeout.
/// </summary>
/// <remarks>
/// Throw a custom exception <see cref="TimeoutExpiredException"/> so the retry policy can handle it.
/// It <i>is-a</i> <see cref="TaskCanceledException"/> which is non-breaking for consumers that handle
/// <see cref="OperationCanceledException"/> or its derived types. Throwing an existing exception type
/// (for ex Polly's timeout policy throws <see cref="Polly.Timeout.TimeoutRejectedException"/>) risks
/// inadvertent retries.
/// </remarks>
public class TimeoutHandler : DelegatingHandler
{
	private readonly ITimeoutHandlerSettings timeoutHandlerSettings;

	/// <inheritdoc cref="TimeoutHandler" />
	public TimeoutHandler(
		ITimeoutHandlerSettings timeoutHandlerSettings)
	{
		this.timeoutHandlerSettings = timeoutHandlerSettings
			?? throw new ArgumentNullException(nameof(timeoutHandlerSettings));
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		using var timeoutCts = new CancellationTokenSource(timeoutHandlerSettings.TimeoutInMilliseconds);
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

		try
		{
			return await base.SendAsync(request, linkedCts.Token)
				.ConfigureAwait(false);
		}
		catch (TaskCanceledException tcex) when (timeoutCts.IsCancellationRequested)
		{
			// if the timeout cts is canceled, throw a (custom) timeout ex
			// note: TaskCanceledException doesn't have a ctor overload for CancellationToken
			//   as OperationCanceledException at the moment.
			throw new TimeoutExpiredException(tcex);
		}
	}
}

public interface ITimeoutHandlerSettings
{
	int TimeoutInMilliseconds { get; }
}

public record class TimeoutHandlerSettings : ITimeoutHandlerSettings
{
	public int TimeoutInMilliseconds { get; init; }
}

[Serializable]
public class TimeoutExpiredException : TaskCanceledException
{
	private const string cannedMessage = "Timeout expired";

	public TimeoutExpiredException() : base(cannedMessage) { }
	public TimeoutExpiredException(string message) : base(message) { }
	public TimeoutExpiredException(Task task) : base(task) { }
	public TimeoutExpiredException(string message, Exception inner) : base(message, inner) { }
	public TimeoutExpiredException(Exception inner) : base(cannedMessage, inner) { }
	protected TimeoutExpiredException(
		System.Runtime.Serialization.SerializationInfo info,
		System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
