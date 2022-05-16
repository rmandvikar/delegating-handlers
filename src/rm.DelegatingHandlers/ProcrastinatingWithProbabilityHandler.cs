using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using rm.FeatureToggle;
using rm.Random2;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Causes delay with a probability to induce http timeouts.
	/// </summary>
	public class ProcrastinatingWithProbabilityHandler : DelegatingHandler
	{
		private readonly IProcrastinatingWithProbabilityHandlerSettings procrastinatingWithProbabilityHandlerSettings;

		private readonly IProbability probability = new Probability(rng);
		private static readonly Random rng = RandomFactory.GetThreadStaticRandom();

		/// <inheritdoc cref="ProcrastinatingWithProbabilityHandler" />
		public ProcrastinatingWithProbabilityHandler(IProcrastinatingWithProbabilityHandlerSettings procrastinatingWithProbabilityHandlerSettings)
		{
			this.procrastinatingWithProbabilityHandlerSettings = procrastinatingWithProbabilityHandlerSettings
				?? throw new ArgumentNullException(nameof(procrastinatingWithProbabilityHandlerSettings));
		}

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			if (probability.IsTrue(procrastinatingWithProbabilityHandlerSettings.ProbabilityPercentage))
			{
				await Task.Delay(procrastinatingWithProbabilityHandlerSettings.DelayInMilliseconds, cancellationToken)
					.ConfigureAwait(false);
			}

			return await base.SendAsync(request, cancellationToken)
				.ConfigureAwait(false);
		}
	}

	public interface IProcrastinatingWithProbabilityHandlerSettings
	{
		double ProbabilityPercentage { get; }
		int DelayInMilliseconds { get; }
	}

	public record class ProcrastinatingWithProbabilityHandlerSettings : IProcrastinatingWithProbabilityHandlerSettings
	{
		public double ProbabilityPercentage { get; init; }
		public int DelayInMilliseconds { get; init; }
	}
}
