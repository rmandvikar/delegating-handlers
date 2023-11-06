using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using rm.FeatureToggle;

namespace rm.DelegatingHandlers;

/// <summary>
/// Throws an exception with a probability.
/// </summary>
public class ThrowingWithProbabilityHandler : DelegatingHandler
{
	private readonly double probabilityPercentage;
	private readonly Exception exception;

	private readonly IProbability probability;

	/// <inheritdoc cref="ThrowingWithProbabilityHandler" />
	public ThrowingWithProbabilityHandler(
		double probabilityPercentage,
		Exception exception,
		Random rng)
	{
		this.probabilityPercentage = probabilityPercentage;
		// funny, no? perhaps, 20% funny? #aurora
		this.exception = exception
			?? throw new ArgumentNullException(nameof(exception));
		_ = rng
			?? throw new ArgumentNullException(nameof(rng));

		probability = new Probability(rng);
	}

	protected override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		if (probability.IsTrue(probabilityPercentage))
		{
			throw exception;
		}

		return base.SendAsync(request, cancellationToken);
	}
}
