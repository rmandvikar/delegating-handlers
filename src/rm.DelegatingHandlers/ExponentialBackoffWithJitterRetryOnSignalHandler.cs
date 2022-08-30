using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Retries on certain conditions with exponential backoff jitter (DecorrelatedJitterBackoffV2).
	/// <para></para>
	/// Retry conditions:
	///   On signal.
	/// </summary>
	/// <remarks>
	/// <see href="https://github.com/App-vNext/Polly/wiki/Retry-with-jitter">source</see>
	/// </remarks>
	public class ExponentialBackoffWithJitterRetryOnSignalHandler : DelegatingHandler
	{
		private readonly IAsyncPolicy<(HttpRequestMessage request, HttpResponseMessage response)> retryPolicy;

		/// <inheritdoc cref="ExponentialBackoffWithJitterRetryOnSignalHandler" />
		public ExponentialBackoffWithJitterRetryOnSignalHandler(
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
				.HandleResult<(HttpRequestMessage request, HttpResponseMessage response)>(tuple =>
					tuple.request.Properties.TryGetValue(RequestProperties.RetrySignal, out var retrySignaledObj) && (bool)retrySignaledObj)
				.WaitAndRetryAsync(
					sleepDurations: sleepDurationsWithJitter,
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
			var tuple = await retryPolicy.ExecuteAsync(
				action: async (context, ct) =>
				{
					if (context.TryGetValue(ContextKey.RetryAttempt, out var retryAttempt))
					{
						request.Properties[RequestProperties.PollyRetryAttempt] = retryAttempt;
					}
					request.Properties.Remove(RequestProperties.RetrySignal);
					try
					{
						var response = await base.SendAsync(request, ct)
							.ConfigureAwait(false);
						return (request, response);
					}
					catch (Exception)
						when (request.Properties.TryGetValue(RequestProperties.RetrySignal, out var retrySignaledObj) && (bool)retrySignaledObj)
					{
						// swallow if retry signaled on ex
						return (request, null);
					}
				},
				context: new Context(),
				cancellationToken: cancellationToken)
					.ConfigureAwait(false);
			return tuple.response;
		}
	}
}
