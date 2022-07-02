using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using rm.FeatureToggle;
using rm.Random2;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Causes a fault window by signaling for a fault action down the handler chain.
	/// </summary>
	public class FaultWindowSignalingHandler : DelegatingHandler
	{
		private readonly IFaultWindowSignalingHandlerSettings faultWindowSignalingHandlerSettings;

		private readonly IProbability probability = new Probability(rng);
		private static readonly Random rng = RandomFactory.GetThreadStaticRandom();

		private readonly object locker = new object();
		private DateTime? faultWindowEndTime = null;

		/// <inheritdoc cref="FaultWindowSignalingHandler" />
		public FaultWindowSignalingHandler(
			IFaultWindowSignalingHandlerSettings faultWindowSignalingHandlerSettings)
		{
			this.faultWindowSignalingHandlerSettings = faultWindowSignalingHandlerSettings;
		}

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{

			var isInFaultWindow = IsInFaultWindow;
			if (!isInFaultWindow && probability.IsTrue(faultWindowSignalingHandlerSettings.ProbabilityPercentage))
			{
				isInFaultWindow = IsInFaultWindow = true;
			}
			if (isInFaultWindow)
			{
				// signal for a fault action down the handler chain
				request.Properties[faultWindowSignalingHandlerSettings.SignalProperty] = true;
			}

			return base.SendAsync(request, cancellationToken);
		}

		private bool IsInFaultWindow
		{
			get
			{
				lock (locker)
				{
					return DateTime.UtcNow <= faultWindowEndTime;
				}
			}
			set
			{
				lock (locker)
				{
					faultWindowEndTime = value
						? DateTime.UtcNow + faultWindowSignalingHandlerSettings.FaultDuration
						: null;
				}
			}
		}
	}

	public interface IFaultWindowSignalingHandlerSettings
	{
		public double ProbabilityPercentage { get; }
		public TimeSpan FaultDuration { get; }
		public string SignalProperty { get; }
	}

	public class FaultWindowSignalingHandlerSettings : IFaultWindowSignalingHandlerSettings
	{
		public double ProbabilityPercentage { get; init; }
		public TimeSpan FaultDuration { get; init; }
		public string SignalProperty { get; init; }
	}
}
