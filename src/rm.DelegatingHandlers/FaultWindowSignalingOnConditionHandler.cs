using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using rm.FeatureToggle;
using rm.Random2;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Causes a fault window on condition by signaling for a fault action down the handler chain.
	/// </summary>
	public class FaultWindowSignalingOnConditionHandler : DelegatingHandler
	{
		private readonly IFaultWindowSignalingOnConditionHandlerSettings faultWindowSignalingOnConditionHandlerSettings;

		private readonly IProbability probability = new Probability(rng);
		private static readonly Random rng = RandomFactory.GetThreadStaticRandom();

		private readonly object locker = new object();
		private DateTime? faultWindowEndTime = null;

		/// <inheritdoc cref="FaultWindowSignalingOnConditionHandler" />
		public FaultWindowSignalingOnConditionHandler(
			IFaultWindowSignalingOnConditionHandlerSettings faultWindowSignalingOnConditionHandlerSettings)
		{
			this.faultWindowSignalingOnConditionHandlerSettings = faultWindowSignalingOnConditionHandlerSettings
				?? throw new ArgumentNullException(nameof(faultWindowSignalingOnConditionHandlerSettings));
		}

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			bool condition;
			if (request.Properties.TryGetValue(typeof(FaultWindowSignalingOnConditionHandler).FullName, out var value)
				&& value is not null
				&& (condition = (bool)value)
				&& condition)
			{
				var isInFaultWindow = IsInFaultWindow;
				if (!isInFaultWindow && probability.IsTrue(faultWindowSignalingOnConditionHandlerSettings.ProbabilityPercentage))
				{
					isInFaultWindow = IsInFaultWindow = true;
				}
				if (isInFaultWindow)
				{
					// signal for a fault action down the handler chain
					request.Properties[faultWindowSignalingOnConditionHandlerSettings.SignalProperty] = true;
				}
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
						? DateTime.UtcNow + faultWindowSignalingOnConditionHandlerSettings.FaultDuration
						: null;
				}
			}
		}
	}

	public interface IFaultWindowSignalingOnConditionHandlerSettings
	{
		public double ProbabilityPercentage { get; }
		public TimeSpan FaultDuration { get; }
		public string SignalProperty { get; }
	}

	public class FaultWindowSignalingOnConditionHandlerSettings : IFaultWindowSignalingOnConditionHandlerSettings
	{
		public double ProbabilityPercentage { get; init; }
		public TimeSpan FaultDuration { get; init; }
		public string SignalProperty { get; init; }
	}
}
