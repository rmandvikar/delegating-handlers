using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;

namespace rm.DelegatingHandlers;

/// <summary>
/// Retries on certain conditions with exponential backoff jitter (DecorrelatedJitterBackoffV2).
/// <para></para>
/// Retry conditions:
///   HttpRequestException, 5xx.
/// </summary>
/// <remarks>
/// <see href="https://github.com/App-vNext/Polly/wiki/Retry-with-jitter">source</see>
/// </remarks>
public class ExponentialBackoffWithJitterRetryHandler : DelegatingHandler
{
	private readonly AsyncRetryPolicy<HttpResponseMessage> retryPolicy;

	/// <inheritdoc cref="ExponentialBackoffWithJitterRetryHandler" />
	public ExponentialBackoffWithJitterRetryHandler(
		IRetrySettings retrySettings)
	{
		_ = retrySettings
			?? throw new ArgumentNullException(nameof(retrySettings));

		var sleepDurationsWithJitter = Backoff.DecorrelatedJitterBackoffV2(
			medianFirstRetryDelay: TimeSpan.FromMilliseconds(retrySettings.RetryDelayInMilliseconds),
			retryCount: retrySettings.RetryCount);

		// note: response can't be null
		// ref: https://github.com/dotnet/runtime/issues/19925#issuecomment-272664671
		retryPolicy = Policy
			.Handle<HttpRequestException>()
			.Or<TimeoutExpiredException>()
			.OrResult<HttpResponseMessage>(response => response.Is5xx())
			.WaitAndRetryAsync(
				sleepDurations: sleepDurationsWithJitter,
				onRetry: (responseResult, delay, retryAttempt, context) =>
				{
					// note: response can be null in case of handled exception
					responseResult.Result?.Dispose();
					context[ContextKey.RetryAttempt] = retryAttempt;
				});
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		return await retryPolicy.ExecuteAsync(
			action: async (context, ct) =>
			{
				var retryAttempt = context.TryGetValue(ContextKey.RetryAttempt, out var retryAttemptObj) ? retryAttemptObj : 0;
				request.Properties[RequestProperties.PollyRetryAttempt] = retryAttempt;
				return await base.SendAsync(request, ct)
					.ConfigureAwait(false);
			},
			context: new Context(),
			cancellationToken: cancellationToken)
				.ConfigureAwait(false);
	}
}

public interface IRetrySettings
{
	int RetryCount { get; }
	int RetryDelayInMilliseconds { get; }
}

public record class RetrySettings : IRetrySettings
{
	public int RetryCount { get; init; }
	public int RetryDelayInMilliseconds { get; init; }
}

internal static class ContextKey
{
	internal const string RetryAttempt = nameof(RetryAttempt);
}
