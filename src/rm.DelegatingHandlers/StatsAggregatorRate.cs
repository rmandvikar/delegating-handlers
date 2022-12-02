using System;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Emits average.
	/// </summary>
	public interface IRateMetricEmitter
	{
		void Emit(
			string metricName,
			double rate);
	}

	public class RateStatsAggregator : StatsAggregatorBase
	{
		private readonly IStatsAggregatorSettings statsAggregatorSettings;
		private readonly IRateMetricEmitter rateMetricEmitter;
		private double sum = 0d;
		private long N = 0;

		public RateStatsAggregator(
			IStatsAggregatorSettings statsAggregatorSettings,
			IRateMetricEmitter rateMetricEmitter,
			Action<Exception> onProcessingError)
			: base(
				  statsAggregatorSettings,
				  onProcessingError)
		{
			this.statsAggregatorSettings = statsAggregatorSettings
				?? throw new ArgumentNullException(nameof(statsAggregatorSettings));
			this.rateMetricEmitter = rateMetricEmitter
				?? throw new ArgumentNullException(nameof(rateMetricEmitter));
		}

		protected override void AggregateStats()
		{
			double? rateForInterval;
			lock (locker)
			{
				rateForInterval = N != 0 ? sum / N : null;
				sum = 0;
				N = 0;
			}
			if (rateForInterval != null)
			{
				rateMetricEmitter.Emit(statsAggregatorSettings.MetricName, rateForInterval.Value);
			}
		}

		public override void Add(double value)
		{
			lock (locker)
			{
				sum += value;
				N += 1;
			}
		}
	}
}
