using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using rm.Extensions;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Measures percentiles for given interval.
	/// </summary>
	/// <remarks>
	/// Interval range is [1, 60] in seconds.
	/// </remarks>
	public class PercentilesMeasuringHandler : DelegatingHandler
	{
		private readonly IPercentilesMeasuringHandlerSettings percentilesMeasuringHandlerSettings;

		private List<long> buffer;
		private const int initialCapacity = 1_024;
		private readonly Timer timer;
		private readonly object locker = new object();
		private const int SecondsInDay = 86_400; // 24h * 60m * 60s
		private const int SecondsInMinute = 60; // 1m * 60s

		/// <inheritdoc cref="PercentilesMeasuringHandler" />
		public PercentilesMeasuringHandler(
			IPercentilesMeasuringHandlerSettings percentilesMeasuringHandlerSettings)
		{
			_ = percentilesMeasuringHandlerSettings
				?? throw new ArgumentNullException(nameof(percentilesMeasuringHandlerSettings));
			if (percentilesMeasuringHandlerSettings.IntervalInSeconds <= 0
				|| percentilesMeasuringHandlerSettings.IntervalInSeconds > SecondsInMinute)
			{
				throw new ArgumentOutOfRangeException(
					nameof(percentilesMeasuringHandlerSettings.IntervalInSeconds),
					$"0 < {percentilesMeasuringHandlerSettings.IntervalInSeconds} <= {SecondsInMinute}");
			}
			this.percentilesMeasuringHandlerSettings = percentilesMeasuringHandlerSettings;

			_ = this.percentilesMeasuringHandlerSettings.PercentilesMeasuringProcessor
				?? throw new ArgumentNullException(nameof(this.percentilesMeasuringHandlerSettings.PercentilesMeasuringProcessor));

			buffer = new List<long>(initialCapacity);
			timer = new Timer(Callback, null, Timeout.Infinite, Timeout.Infinite);
			// setup
			StartTimer();
		}

		private void StartTimer()
		{
			if (disposed)
			{
				return;
			}

			// bin intervals
			// recalculate due to time creep
			var nextIntervalDelayInMilliseconds =
				CalculateNextIntervalDelay(percentilesMeasuringHandlerSettings.IntervalInSeconds);
			// schedule
			timer.Change(nextIntervalDelayInMilliseconds, Timeout.Infinite);
		}

		/// <remarks>
		/// Bin intervals, so the events are fired at the start of bins.
		///
		/// So,
		/// for  1s intervals, event is fired at second 00, 01, 02, etc.
		/// for  5s intervals, event is fired at second 00, 05, 10, etc.
		/// for 10s intervals, event is fired at second 00, 10, 20, etc.
		/// for 15s intervals, event is fired at second 00, 15, 30, etc.
		///
		/// The interval is calculated, and fired with range (0, interval].
		/// </remarks>
		internal int CalculateNextIntervalDelay(int intervalInSeconds)
		{
			const int MillisecondsInDay = SecondsInDay * 1000;
			var intervalInMilliseconds = intervalInSeconds * 1000;
			var now = DateTime.Now;
			var millisecondsSinceToday = (int)now.TimeOfDay.TotalMilliseconds;
			if (millisecondsSinceToday > MillisecondsInDay)
			{
				throw new InvalidOperationException($"millisecondsSinceToday > MillisecondsInDay. millisecondsSinceToday: {millisecondsSinceToday}, MillisecondsInDay: {MillisecondsInDay}");
			}
			// bin(n, interval) gives current bin
			var nextIntervalMillisecondsSinceToday =
				millisecondsSinceToday.Bin(intervalInMilliseconds) + intervalInMilliseconds;
			var intervalDelayInMilliseconds = nextIntervalMillisecondsSinceToday - millisecondsSinceToday;
			// pad the delay to slightly push it over
			const int intervalDelayPaddingInMilliseconds = 10;
			intervalDelayInMilliseconds += intervalDelayPaddingInMilliseconds;
			return intervalDelayInMilliseconds;
		}

		private void Callback(object state)
		{
			try
			{
				MeasurePercentiles();
			}
			catch (Exception ex)
			{
				// swallow
				percentilesMeasuringHandlerSettings.OnProcessingError?.Invoke(ex);
			}
			finally
			{
				// setup
				StartTimer();
			}
		}

		private void MeasurePercentiles()
		{
			List<long> sequence;
			lock (locker)
			{
				// read buffer ref
				sequence = buffer;
				var capacity = CalculateCapacity(sequence.Count, sequence.Capacity);
				buffer = new List<long>(capacity);
			}

			var N = sequence.Count;
			// shortcircuit when N = 0
			if (N == 0)
			{
				return;
			}

			double p0 = 0, p50 = 0, p90 = 0, p95 = 0, p99 = 0, p999 = 0, p9999 = 0, p100 = 0;
			sequence.Sort();
			Parallel.Invoke(
				() => p0 = Percentile(sequence, N, 0.0d),
				() => p50 = Percentile(sequence, N, 0.50d),
				() => p90 = Percentile(sequence, N, 0.90d),
				() => p95 = Percentile(sequence, N, 0.95d),
				() => p99 = Percentile(sequence, N, 0.99d),
				() => p999 = Percentile(sequence, N, 0.999d),
				() => p9999 = Percentile(sequence, N, 0.9999d),
				() => p100 = Percentile(sequence, N, 1.0d)
				);

			percentilesMeasuringHandlerSettings.PercentilesMeasuringProcessor.Process(
				percentilesMeasuringHandlerSettings.MetricName, N, p0, p50, p90, p95, p99, p999, p9999, p100);
		}

		private int CalculateCapacity(int count, int capacity)
		{
			if (count <= initialCapacity)
			{
				return initialCapacity;
			}
			var trimmedCapacity = capacity;
			while ((trimmedCapacity >> 1) > count)
			{
				trimmedCapacity >>= 1;
			}
			return trimmedCapacity;
		}

		/// <summary>
		/// Calculates percentiles for given sorted <paramref name="sequence"/>.
		/// <para/>
		/// <remarks>
		/// <paramref name="sequence"/> is sorted. <paramref name="N"/> > 0.
		/// </remarks>
		/// <para/>
		/// <see href="https://stackoverflow.com/questions/8137391/percentile-calculation">source</see>
		/// </summary>
		public double Percentile(List<long> sequence, int N, double percentile)
		{
			if (N <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(N), $"N > 0. N: {N}");
			}
			double n = (N - 1) * percentile + 1;
			// Another method: double n = (N + 1) * percentile;
			if (n == 1d)
			{
				return sequence[0];
			}
			else if (n == N)
			{
				return sequence[N - 1];
			}
			else
			{
				int k = (int)n;
				double d = n - k;
				return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
			}
		}

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			var stopwatch = Stopwatch.StartNew();
			try
			{
				return await base.SendAsync(request, cancellationToken)
					.ConfigureAwait(false);
			}
			finally
			{
				stopwatch.Stop();
				lock (locker)
				{
					buffer.Add(stopwatch.ElapsedMilliseconds);
				}
			}
		}

		private void StopTimer()
		{
			if (disposed)
			{
				return;
			}

			timer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		private bool disposed = false;

		protected override void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					// current interval
					// stop timer to avoid race
					StopTimer();
					try
					{
						MeasurePercentiles();
					}
					catch (Exception ex)
					{
						// swallow
						percentilesMeasuringHandlerSettings.OnProcessingError?.Invoke(ex);
					}

					timer?.Dispose();

					buffer = null;

					disposed = true;
				}
			}
		}
	}

	public interface IPercentilesMeasuringHandlerSettings
	{
		string MetricName { get; }
		int IntervalInSeconds { get; }
		IPercentilesMeasuringProcessor PercentilesMeasuringProcessor { get; }
		Action<Exception> OnProcessingError { get; }
	}

	public interface IPercentilesMeasuringProcessor
	{
		void Process(
			string metricName,
			int N,
			double p0,
			double p50, double p90, double p95, double p99,
			double p999, double p9999,
			double p100);
	}

	public class PercentilesMeasuringHandlerSettings : IPercentilesMeasuringHandlerSettings
	{
		public string MetricName { get; init; }
		public int IntervalInSeconds { get; init; }
		public IPercentilesMeasuringProcessor PercentilesMeasuringProcessor { get; init; }
		public Action<Exception> OnProcessingError { get; init; }
	}
}
