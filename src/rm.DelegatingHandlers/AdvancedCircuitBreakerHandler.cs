using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace rm.DelegatingHandlers;

/// <summary>
/// Circuit breaks on certain conditions.
/// <para></para>
/// Circuit breaker conditions:
///   TaskCanceledException, 429,
///   HttpRequestException, 5xx, TimeoutExpiredException
/// </summary>
/// <remarks>
/// Uses polly's AdvancedCircuitBreaker.
///
/// <see href="https://github.com/App-vNext/Polly/wiki/Advanced-Circuit-Breaker">source</see>
/// </remarks>
public class AdvancedCircuitBreakerHandler : DelegatingHandler
{
	private readonly IAsyncPolicy<HttpResponseMessage> advancedCircuitBreakerPolicy;

	/// <inheritdoc cref="AdvancedCircuitBreakerHandler" />
	public AdvancedCircuitBreakerHandler(
		IAdvancedCircuitBreakerHandlerSettings advancedCircuitBreakerHandlerSettings)
	{
		_ = advancedCircuitBreakerHandlerSettings
			?? throw new ArgumentNullException(nameof(advancedCircuitBreakerHandlerSettings));

		// see
		// circuitBreaker: https://github.com/App-vNext/Polly/wiki/Circuit-Breaker
		// advancedCircuitBreaker: https://github.com/App-vNext/Polly/wiki/Advanced-Circuit-Breaker
		// note:
		// - with 24 minimumThroughput, and 5 machines, the min tpm required for
		//   all circuitBreakers' stats to kick in is 120 tpm.
		// - with X tpm, 50% failureThreshold, 1m samplingDuration, and X minimumThroughput,
		//   if calls go from 100% success to 100% failure, the circuit will open in 30s.
		// - with 6s durationOfBreak, and 5 machines, the max tpm allowed when
		//   circuit is open is 50 tpm.
		// - with high durationOfBreak (ex 30s), there is a risk of leaving the circuit open
		//   too long.
		// - in app, convert circuitBreaker's brokenCircuitEx to 429 too many requests so its
		//   upstream dependency can in turn circuit break. app may add a "retry-after" header.
		advancedCircuitBreakerPolicy = Policy
			// circuitBreaker conditions must be a superset of retry conditions, and
			// taskCanceledEx, 429.
			// note:
			//   with circuitBreaker, one "may" retry on taskCanceledEx, 429 too with caution.
			//   for retry on 429, honor "retry-after" header.
			.Handle<TaskCanceledException>()
			.Or<HttpRequestException>()
			.Or<TimeoutExpiredException>()
			.OrResult<HttpResponseMessage>(response =>
				// 5xx, 429 too many requests
				response.Is5xx()
				|| response.StatusCode == (HttpStatusCode)429)
			.AdvancedCircuitBreakerAsync(
				// ex: .5; 50% to 70% are good threshold numbers (see advancedCircuitBreaker)
				failureThreshold: advancedCircuitBreakerHandlerSettings.FailureThreshold,
				// ex: 1m; sample 1m for ease of calculation
				samplingDuration: advancedCircuitBreakerHandlerSettings.SamplingDuration,
				// ex: 24 (*5 machineCount); require minimumThroughput *machineCount tpm
				minimumThroughput: advancedCircuitBreakerHandlerSettings.MinimumThroughput,
				// ex: 6s; allow 10 *machineCount tpm when circuit is open
				durationOfBreak: advancedCircuitBreakerHandlerSettings.DurationOfBreak);
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		return await advancedCircuitBreakerPolicy.ExecuteAsync(
			action: async (ct) =>
			{
				return await base.SendAsync(request, ct)
					.ConfigureAwait(false);
			},
			cancellationToken: cancellationToken)
				.ConfigureAwait(false);
	}
}

public interface IAdvancedCircuitBreakerHandlerSettings
{
	double FailureThreshold { get; }
	TimeSpan SamplingDuration { get; }
	int MinimumThroughput { get; }
	TimeSpan DurationOfBreak { get; }
}

public record class AdvancedCircuitBreakerHandlerSettings : IAdvancedCircuitBreakerHandlerSettings
{
	public double FailureThreshold { get; init; }
	public TimeSpan SamplingDuration { get; init; }
	public int MinimumThroughput { get; init; }
	public TimeSpan DurationOfBreak { get; init; }
}
