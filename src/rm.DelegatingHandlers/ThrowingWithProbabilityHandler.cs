using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using rm.FeatureToggle;
using rm.Random2;

namespace rm.DelegatingHandlers;

/// <summary>
/// Throws an exception with a probability.
/// </summary>
public class ThrowingWithProbabilityHandler : DelegatingHandler
{
	private readonly double probabilityPercentage;
	private readonly Exception exception;

	private readonly IProbability probability = new Probability(rng);
	private static readonly Random rng = RandomFactory.GetThreadStaticRandom();

	/// <inheritdoc cref="ThrowingWithProbabilityHandler" />
	public ThrowingWithProbabilityHandler(
		double probabilityPercentage,
		Exception exception)
	{
		this.probabilityPercentage = probabilityPercentage;
		// funny, no? perhaps, 20% funny? #aurora
		this.exception = exception
			?? throw new ArgumentNullException(nameof(exception));
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
