﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using rm.Extensions;

namespace rm.DelegatingHandlers;

/// <summary>
/// Retries on certain conditions with exponential backoff jitter (DecorrelatedJitterBackoffV2).
/// <para></para>
/// Retry conditions:
///   HttpRequestException, 5xx, 429 (see retry-after header below).
///   <br/>
/// retry-after header:
///   <br/>
///   For 503: retry honoring header if present, else retry as usual.
///   <br/>
///   For 429: retry honoring header only if present, else do not retry.
/// </summary>
/// <remarks>
/// <see href="https://github.com/App-vNext/Polly/wiki/Retry-with-jitter">source</see>
/// </remarks>
public class ExponentialBackoffWithJitterRetryHandler : DelegatingHandler
{
	private readonly AsyncRetryPolicy<(HttpResponseMessage response, Context Context)> retryPolicy;
	private readonly IRetrySettings retrySettings;

	/// <inheritdoc cref="ExponentialBackoffWithJitterRetryHandler" />
	public ExponentialBackoffWithJitterRetryHandler(
		IRetrySettings retrySettings)
	{
		this.retrySettings = retrySettings
			?? throw new ArgumentNullException(nameof(retrySettings));

		// note: response can't be null
		// ref: https://github.com/dotnet/runtime/issues/19925#issuecomment-272664671
		retryPolicy = Policy
			.Handle<HttpRequestException>()
			.Or<TimeoutExpiredException>()
			.OrResult<(HttpResponseMessage response, Context context)>(tuple =>
			{
				// handle retry-after response header if present
				return CanRetry(tuple.response, tuple.context);
			})
			.WaitAndRetryAsync(
				retryCount: retrySettings.RetryCount,
				sleepDurationProvider: (retryAttempt, responseResult, context) =>
				{
					// note: result can be null in case of handled exception
					// retryAttempt is 1-based
					var sleepDurationWithJitter = ((TimeSpan[])context[ContextKey.SleepDurations])[retryAttempt - 1];
					return sleepDurationWithJitter;
				},
				onRetry: (responseResult, delay, retryAttempt, context) =>
				{
					// note: response can be null in case of handled exception
					responseResult.Result.response?.Dispose();
					context[ContextKey.RetryAttempt] = retryAttempt;
				});
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		// read the retry delays upfront
		var sleepDurationsWithJitter = Backoff.DecorrelatedJitterBackoffV2(
			medianFirstRetryDelay: TimeSpan.FromMilliseconds(retrySettings.RetryDelayInMilliseconds),
			retryCount: retrySettings.RetryCount).ToArray();
		var context = new Context();
		context[ContextKey.SleepDurations] = sleepDurationsWithJitter;

		var tuple = await retryPolicy.ExecuteAsync(
			action: async (context, ct) =>
			{
				if (context.TryGetValue(ContextKey.RetryAttempt, out var retryAttempt))
				{
					request.Properties[RequestProperties.PollyRetryAttempt] = retryAttempt;
				}
				var response = await base.SendAsync(request, ct)
					.ConfigureAwait(false);
				return (response, context);
			},
			context: context,
			cancellationToken: cancellationToken)
				.ConfigureAwait(false);
		return tuple.response;
	}

	private static bool CanRetry(
		HttpResponseMessage response,
		Context context)
	{
		var sleepDurationsWithJitter = (TimeSpan[])context[ContextKey.SleepDurations];
		if (sleepDurationsWithJitter.IsEmpty())
		{
			return false;
		}
		// retryAttempt is 0-based
		var retryAttempt = context.TryGetValue(ContextKey.RetryAttempt, out object retryAttemptObj) ? (int)retryAttemptObj : 0;
		if (retryAttempt == sleepDurationsWithJitter.Count())
		{
			return false;
		}
		var sleepDurationWithJitter = sleepDurationsWithJitter[retryAttempt];

		var statusCode = (int)response.StatusCode;

		// here be dragons
		var retry = false;
		// retry on 5xx, 429 only
		if (response.Is5xx() || statusCode == 429)
		{
			// retry on 503, 429 looking at retry-after value
			if (statusCode == 503 || statusCode == 429)
			{
				// note: look at retry-after value but don't use it to avoid surges at same time;
				// use it to determine whether to retry or not
				var isRetryAfterPresent = response.Headers.TryGetValue(ResponseHeaders.RetryAfter, out var retryAfterValue)
					&& retryAfterValue != null;

#if DEBUG
				Console.WriteLine($"retryAfterValue: {retryAfterValue}");
#endif

				// retry on 503, 429 only on valid retry-after value
				if (isRetryAfterPresent)
				{
					TimeSpan retryAfter;
					if
						(((double.TryParse(retryAfterValue, out double retryAfterDelay)
							// ignore network latency; delay could be 0
							&& Math.Max((retryAfter = TimeSpan.FromSeconds(retryAfterDelay)).TotalSeconds, 0) >= 0)
						||
						(DateTimeOffset.TryParse(retryAfterValue, out DateTimeOffset retryAfterDate)
							// date could be in the past due to network latency
							&& Math.Max((retryAfter = retryAfterDate - DateTimeOffset.UtcNow).TotalSeconds, 0) >= 0))
						// only retry if delay is at or above retry-after value
						&& retryAfter <= sleepDurationWithJitter)
					{
						retry = true;
					}
				}
				else
				{
					// retry on 503 if retry-after not present as typical
					if (statusCode == 503)
					{
						retry = true;
					}
				}
			}
			else
			{
				// retry on 5xx (other than 503) as typical
				retry = true;
			}
		}

#if DEBUG
		Console.WriteLine($"sleepDurationWithJitter: {sleepDurationWithJitter}");
		Console.WriteLine($"retry: {retry}");
#endif

		return retry;
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
	internal const string SleepDurations = nameof(SleepDurations);
}
