using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using rm.FeatureToggle;
using rm.Random2;

namespace rm.DelegatingHandlers;

/// <summary>
/// Causes http response for given statusCode, content with a probability.
/// </summary>
public class ShortCircuitingResponseWithProbabilityHandler : DelegatingHandler
{
	private readonly IShortCircuitingResponseWithProbabilityHandlerSettings shortCircuitingResponseWithProbabilityHandlerSettings;

	private readonly IProbability probability = new Probability(rng);
	private static readonly Random rng = RandomFactory.GetThreadStaticRandom();

	/// <inheritdoc cref="ShortCircuitingResponseWithProbabilityHandler" />
	public ShortCircuitingResponseWithProbabilityHandler(
		IShortCircuitingResponseWithProbabilityHandlerSettings shortCircuitingResponseWithProbabilityHandlerSettings)
	{
		this.shortCircuitingResponseWithProbabilityHandlerSettings = shortCircuitingResponseWithProbabilityHandlerSettings
			?? throw new ArgumentNullException(nameof(shortCircuitingResponseWithProbabilityHandlerSettings));
	}

	protected override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		if (probability.IsTrue(shortCircuitingResponseWithProbabilityHandlerSettings.ProbabilityPercentage))
		{
			return Task.FromResult(
				new HttpResponseMessage
				{
					StatusCode = shortCircuitingResponseWithProbabilityHandlerSettings.StatusCode,
					ReasonPhrase = $"{nameof(ShortCircuitingResponseWithProbabilityHandler)} says hello!",
					Content =
						shortCircuitingResponseWithProbabilityHandlerSettings.Content == null ?
						null :
						new StringContent(
							shortCircuitingResponseWithProbabilityHandlerSettings.Content,
							Encoding.UTF8,
							MediaTypeNames.Application.Json),
				});
		}

		return base.SendAsync(request, cancellationToken);
	}
}

public interface IShortCircuitingResponseWithProbabilityHandlerSettings
{
	double ProbabilityPercentage { get; }
	HttpStatusCode StatusCode { get; }
	string Content { get; }
}

public record class ShortCircuitingResponseWithProbabilityHandlerSettings : IShortCircuitingResponseWithProbabilityHandlerSettings
{
	public double ProbabilityPercentage { get; init; }
	public HttpStatusCode StatusCode { get; init; }
	public string Content { get; init; }
}
