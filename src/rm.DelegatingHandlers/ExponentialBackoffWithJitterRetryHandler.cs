using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Contrib.WaitAndRetry;
using rm.Extensions;

namespace rm.DelegatingHandlers
{
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
	///   For 429: retry only if header present, and conditions are met.
	/// </summary>
	/// <remarks>
	/// <see href="https://github.com/App-vNext/Polly/wiki/Retry-with-jitter">source</see>
	/// </remarks>
	public class ExponentialBackoffWithJitterRetryHandler : DelegatingHandler
	{
		private readonly IAsyncPolicy<HttpResponseMessage> retryPolicy;
		private readonly IRetrySettings retrySettings;

		/// <inheritdoc cref="ExponentialBackoffWithJitterRetryHandler" />
		public ExponentialBackoffWithJitterRetryHandler(
			IRetrySettings retrySettings)
		{
			this.retrySettings = retrySettings
				?? throw new ArgumentNullException(nameof(retrySettings));

			var sleepDurationsWithJitter = Backoff.DecorrelatedJitterBackoffV2(
				medianFirstRetryDelay: TimeSpan.FromMilliseconds(retrySettings.RetryDelayInMilliseconds),
				retryCount: retrySettings.RetryCount);

			var maxRetryDelayInMilliseconds = Backoff.ExponentialBackoff(
				initialDelay: TimeSpan.FromMilliseconds(retrySettings.RetryDelayInMilliseconds),
				retryCount: retrySettings.RetryCount)
				.Last().TotalMilliseconds;

			// note: response can't be null
			// ref: https://github.com/dotnet/runtime/issues/19925#issuecomment-272664671
			retryPolicy = Policy
				.Handle<HttpRequestException>()
				.Or<TimeoutExpiredException>()
				.OrResult<HttpResponseMessage>(response =>
				{
					// handle retry-after response header if present
					var retryAfterValue = (string)null;
					var hasRetryAfter = response.Headers.TryGetValues(ResponseHeaders.RetryAfter, out var retryAfterValues)
						&& (retryAfterValue = retryAfterValues.OneOrDefault()) != null;
					var isRetryAfterWithinRetryWindow = (bool?)null;
					if (hasRetryAfter)
					{
						isRetryAfterWithinRetryWindow =
							(double.TryParse(retryAfterValue, out double retryAfterSeconds)
								// ignore network latency
								&& TimeSpan.FromSeconds(retryAfterSeconds).TotalMilliseconds <= maxRetryDelayInMilliseconds)
							||
							(DateTimeOffset.TryParse(retryAfterValue, out DateTimeOffset retryAfterDate)
								&& (retryAfterDate - DateTimeOffset.UtcNow).TotalMilliseconds <= maxRetryDelayInMilliseconds);
					}
					var statusCode = (int)response.StatusCode;
					return
						(response.Is5xx() && statusCode != 503)
							|| (statusCode == 503 && (!hasRetryAfter || (hasRetryAfter && isRetryAfterWithinRetryWindow.Value)))
							// retry 429 only if retry-after response header present
							|| (statusCode == 429 && hasRetryAfter && isRetryAfterWithinRetryWindow.Value);
				})
				.WaitAndRetryAsync(
					retryCount: retrySettings.RetryCount,
					sleepDurationProvider: (retryAttempt, responseResult, context) =>
					{
						// handle retry-after response header if present
						var retryAfter = TimeSpan.Zero;
						var isRetryAfterValid = false;
						// note: response can be null in case of handled exception
						var response = responseResult.Result;
						if (response != null)
						{
							var retryAfterValue = (string)null;
							var statusCode = (int)response.StatusCode;
							isRetryAfterValid =
								(statusCode == 503
									|| statusCode == 429)
								&& response.Headers.TryGetValues(ResponseHeaders.RetryAfter, out var retryAfterValues)
									&& (retryAfterValue = retryAfterValues.OneOrDefault()) != null
								&&
									((double.TryParse(retryAfterValue, out double retryAfterSeconds)
										// ignore network latency
										&& (retryAfter = TimeSpan.FromSeconds(retryAfterSeconds)).TotalSeconds > 0)
									||
									(DateTimeOffset.TryParse(retryAfterValue, out DateTimeOffset retryAfterDate)
										&& (retryAfter = retryAfterDate - DateTimeOffset.UtcNow).TotalSeconds > 0));
						}
						var retryIndex = retryAttempt - 1;
						var sleepDurationWithJitter = sleepDurationsWithJitter;
						return isRetryAfterValid
							// wait for higher of the two values
							? MathHelper.Max(retryAfter, sleepDurationWithJitter)
							: sleepDurationWithJitter;
					},
					onRetry: (responseResult, delay, retryAttempt, context) =>
					{
						responseResult.Result?.Dispose();

						context[ContextKey.RetryAttempt] = retryAttempt;
					});
		}

		protected async override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			return await retryPolicy.ExecuteAsync(
				action: async (context, ct) =>
				{
					if (context.TryGetValue(ContextKey.RetryAttempt, out var retryAttempt))
					{
						request.Properties[RequestProperties.PollyRetryAttempt] = retryAttempt;
					}

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
}
