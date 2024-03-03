using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// TODO
/// </summary>
public class TokenBucketRetryHandler : DelegatingHandler
{
	private readonly ITokenBucketRetryHandlerSettings tokenBucketRetryHandlerSettings;

	private long callsCount;
	private long retryCallsCount;

	/// <inheritdoc cref="TokenBucketRetryHandler" />
	public TokenBucketRetryHandler(
		ITokenBucketRetryHandlerSettings tokenBucketRetryHandlerSettings)
	{
		this.tokenBucketRetryHandlerSettings = tokenBucketRetryHandlerSettings
			?? throw new ArgumentNullException(nameof(tokenBucketRetryHandlerSettings));
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var calls = Interlocked.Increment(ref callsCount);
		var retryAttempt = (int)request.Properties[RequestProperties.PollyRetryAttempt];
		double percentage = 0;
		if (retryAttempt >= 1)
		{
			var retryCalls = Interlocked.Increment(ref retryCallsCount);
			if (calls > 0
				//&& calls > tokenBucketRetryHandlerSettings.MinimumVolume
				&& (percentage = retryCalls / (double)calls) > tokenBucketRetryHandlerSettings.Percentage)
			{
				throw new TokenBucketRetryException(
					$"percentage (threshold): {tokenBucketRetryHandlerSettings.Percentage}, but was percentage: {percentage}");
			}
		}
#if DEBUG
		Console.WriteLine($"percentage (threshold): {tokenBucketRetryHandlerSettings.Percentage}, but was percentage: {percentage}");
#endif

		var response = await base.SendAsync(request, cancellationToken)
			.ConfigureAwait(false);

		if (retryAttempt >= 1)
		{
			Interlocked.Decrement(ref retryCallsCount);
		}
		Interlocked.Decrement(ref callsCount);

		return response;
	}
}

public interface ITokenBucketRetryHandlerSettings
{
	double Percentage { get; }
	double MinimumVolume { get; }
}

public record class TokenBucketRetryHandlerSettings : ITokenBucketRetryHandlerSettings
{
	public double Percentage { get; init; }
	public double MinimumVolume { get; init; }
}

[Serializable]
public class TokenBucketRetryException : Exception
{
	public TokenBucketRetryException() { }
	public TokenBucketRetryException(string message) : base(message) { }
	public TokenBucketRetryException(string message, Exception inner) : base(message, inner) { }
	protected TokenBucketRetryException(
	  System.Runtime.Serialization.SerializationInfo info,
	  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
