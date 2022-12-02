using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Emits histogram (N, sum, avg, important percentiles).
	/// </summary>
	public interface IHistogramMetricEmitter
	{
		void Emit(
			string metricName,
			int N,
			double sum,
			double avg,
			double p0,
			double p50, double p90, double p95, double p99,
			double p999, double p9999,
			double p100);
	}

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	/// <remarks>
	/// Uses buffer to hold interval values.
	/// </remarks>
	public class HistogramStatsAggregator : StatsAggregatorBase
	{
		private readonly IStatsAggregatorSettings statsAggregatorSettings;
		private readonly IStatsCalculator statsCalculator;
		private readonly IHistogramMetricEmitter histogramMetricEmitter;

		private List<double> buffer;
		private const int initialCapacity = 1_024;

		public HistogramStatsAggregator(
			IStatsAggregatorSettings statsAggregatorSettings,
			IStatsCalculator statsCalculator,
			IHistogramMetricEmitter histogramMetricEmitter,
			Action<Exception> onProcessingError)
			: base(
				  statsAggregatorSettings,
				  onProcessingError)
		{
			_ = statsAggregatorSettings
				?? throw new ArgumentNullException(nameof(statsAggregatorSettings));
			if (statsAggregatorSettings.IntervalInSeconds <= 0
				|| statsAggregatorSettings.IntervalInSeconds > SecondsInMinute)
			{
				throw new ArgumentOutOfRangeException(
					nameof(statsAggregatorSettings.IntervalInSeconds),
					$"0 < {statsAggregatorSettings.IntervalInSeconds} <= {SecondsInMinute}");
			}
			this.statsAggregatorSettings = statsAggregatorSettings;
			this.statsCalculator = statsCalculator
				?? throw new ArgumentNullException(nameof(statsCalculator));
			this.histogramMetricEmitter = histogramMetricEmitter
				?? throw new ArgumentNullException(nameof(histogramMetricEmitter));

			buffer = new List<double>(initialCapacity);
		}

		protected override void AggregateStats()
		{
			List<double> sequence;
			lock (locker)
			{
				var N = buffer.Count;
				// shortcircuit when N = 0
				if (N == 0)
				{
					return;
				}

				// read buffer ref
				sequence = buffer;
				var capacity = CalculateCapacity(sequence.Count, sequence.Capacity);
				buffer = new List<double>(capacity);
			}

			AggregateStats(sequence);
		}

		private void AggregateStats(List<double> sequence)
		{
			var N = sequence.Count;
			double sum = 0, avg = 0;
			double p0 = 0, p50 = 0, p90 = 0, p95 = 0, p99 = 0, p999 = 0, p9999 = 0, p100 = 0;
			sequence.Sort();
			Parallel.Invoke(
				() => sum = statsCalculator.Sum(sequence),
				() => avg = statsCalculator.Average(sequence),
				// percentiles
				() => p0 = statsCalculator.Percentile(sequence, 0.0d),
				() => p50 = statsCalculator.Percentile(sequence, 0.50d),
				() => p90 = statsCalculator.Percentile(sequence, 0.90d),
				() => p95 = statsCalculator.Percentile(sequence, 0.95d),
				() => p99 = statsCalculator.Percentile(sequence, 0.99d),
				() => p999 = statsCalculator.Percentile(sequence, 0.999d),
				() => p9999 = statsCalculator.Percentile(sequence, 0.9999d),
				() => p100 = statsCalculator.Percentile(sequence, 1.0d)
				);

			histogramMetricEmitter.Emit(
				statsAggregatorSettings.MetricName,
				N,
				sum,
				avg,
				// percentiles
				p0,
				p50, p90, p95, p99,
				p999, p9999,
				p100);
		}

		private int CalculateCapacity(int count, int capacity)
		{
			if (count > capacity)
			{
				throw new ArgumentOutOfRangeException(nameof(count),
					$"count <= capacity. count: {count}, capacity: {capacity}");
			}
			if (count <= initialCapacity)
			{
				return initialCapacity;
			}
			var trimmedCapacity = capacity;
			// 2^x-1 < count <= 2^x
			while ((trimmedCapacity >> 1) > count)
			{
				trimmedCapacity >>= 1;
			}
			return trimmedCapacity;
		}

		public override void Add(double value)
		{
			lock (locker)
			{
				buffer.Add(value);
			}
		}

		private bool disposed = false;

		protected override void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					buffer = null;

					disposed = true;
				}
			}

			base.Dispose(disposing);
		}
	}
}
