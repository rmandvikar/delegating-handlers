using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using rm.Extensions;

namespace rm.DelegatingHandlers;

/// <summary>
/// Causes delay as per Gaussian distribution.
/// </summary>
/// <remarks>
/// Use for load testing to short-circuit calls to a dep with Gaussian delay.
/// </remarks>
public class ProcrastinatingGaussianHandler : DelegatingHandler
{
	private readonly double mu;
	private readonly double sigma;
	private readonly Random rng;

	/// <inheritdoc cref="ProcrastinatingGaussianHandler" />
	public ProcrastinatingGaussianHandler(
		IProcrastinatingGaussianHandlerSettings procrastinatingGaussianHandlerSettings,
		Random rng)
	{
		_ = procrastinatingGaussianHandlerSettings
			?? throw new ArgumentNullException(nameof(procrastinatingGaussianHandlerSettings));
		this.rng = rng
			?? throw new ArgumentNullException(nameof(rng));

		mu = procrastinatingGaussianHandlerSettings.Mu;
		sigma = procrastinatingGaussianHandlerSettings.Sigma;
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var delay = rng.NextGaussian(mu, sigma);
		// delay cannot be < 0, so use mu
		// note: if < 0, use mu instead of 0 so the bell curve does not skew toward the 0 bin
		if (delay < 0)
		{
			delay = mu;
		}

		await Task.Delay((int)delay, cancellationToken)
			.ConfigureAwait(false);

		return await base.SendAsync(request, cancellationToken)
			.ConfigureAwait(false);
	}
}

public interface IProcrastinatingGaussianHandlerSettings
{
	double Mu { get; }
	double Sigma { get; }
}

public record class ProcrastinatingGaussianHandlerSettings : IProcrastinatingGaussianHandlerSettings
{
	public double Mu { get; init; }
	public double Sigma { get; init; }
}
