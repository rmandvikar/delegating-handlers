using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using rm.Extensions;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Measures throughput for given interval.
	/// </summary>
	/// <remarks>
	/// Interval range is [1, 86,400] in seconds.
	/// </remarks>
	public class ThroughputMeasuringHandler : DelegatingHandler
	{
		private readonly IThroughputMeasuringHandlerSettings throughputMeasuringHandlerSettings;

		private long i = 0;
		private readonly System.Timers.Timer timer;
		private const int SecondsInDay = 86_400; // 24h * 60m * 60s

		/// <inheritdoc cref="ThroughputMeasuringHandler" />
		public ThroughputMeasuringHandler(
			IThroughputMeasuringHandlerSettings throughputMeasuringHandlerSettings)
		{
			_ = throughputMeasuringHandlerSettings
				?? throw new ArgumentNullException(nameof(throughputMeasuringHandlerSettings));
			if (throughputMeasuringHandlerSettings.IntervalInSeconds <= 0
				|| throughputMeasuringHandlerSettings.IntervalInSeconds > SecondsInDay)
			{
				throw new ArgumentOutOfRangeException(
					nameof(throughputMeasuringHandlerSettings.IntervalInSeconds),
					$"0 < {throughputMeasuringHandlerSettings.IntervalInSeconds} <= {SecondsInDay}");
			}
			this.throughputMeasuringHandlerSettings = throughputMeasuringHandlerSettings;

			timer = new System.Timers.Timer();
			// setup
			SetupTimer();
		}

		private void SetupTimer()
		{
			timer.AutoReset = false;
			timer.Elapsed += OnElapsedEvent!;

			// bin intervals
			var nextIntervalDelayInMilliseconds =
				CalculateNextIntervalDelay(throughputMeasuringHandlerSettings.IntervalInSeconds);
			timer.Interval = nextIntervalDelayInMilliseconds;

			timer.Start();
		}

		/// <remarks>
		/// Bin intervals, so the events are fired at the start of bins.
		///
		/// So,
		/// for  1m intervals, event is fired at minute 00, 01, 02, etc.
		/// for  5m intervals, event is fired at minute 00, 05, 10, etc.
		/// for 10m intervals, event is fired at minute 00, 10, 20, etc.
		/// for 15m intervals, event is fired at minute 00, 15, 30, etc.
		///
		/// The interval is calculated, and fired with range (0, interval].
		/// </remarks>
		internal int CalculateNextIntervalDelay(int intervalInSeconds)
		{
			const int MillisecondsInDay = SecondsInDay * 1000;
			var intervalInMilliseconds = intervalInSeconds * 1000;
			var millisecondsSinceToday = (int)DateTime.Now.TimeOfDay.TotalMilliseconds;
			if (millisecondsSinceToday > MillisecondsInDay)
			{
				throw new InvalidOperationException($"millisecondsSinceToday > MillisecondsInDay. millisecondsSinceToday: {millisecondsSinceToday}, MillisecondsInDay: {MillisecondsInDay}");
			}
			// bin(n, interval) gives current bin
			var nextIntervalMillisecondsSinceToday =
				millisecondsSinceToday.Bin(intervalInMilliseconds) + intervalInMilliseconds;
			var intervalDelayInMilliseconds = nextIntervalMillisecondsSinceToday - millisecondsSinceToday;
			return intervalDelayInMilliseconds;
		}

		private void OnElapsedEvent(object source, ElapsedEventArgs e)
		{
			try
			{
				MeasureThroughput();
			}
			finally
			{
				// recalculate due to time creep
				var nextIntervalDelayInMilliseconds =
					CalculateNextIntervalDelay(throughputMeasuringHandlerSettings.IntervalInSeconds);
				timer.Interval = nextIntervalDelayInMilliseconds;

				timer.Start();
			}
		}

		private void MeasureThroughput()
		{
			var throughput = unchecked((ulong)Interlocked.Exchange(ref i, 0));
			// call even if throughput is 0
			throughputMeasuringHandlerSettings.ThroughputProcessor.Process(throughput);
		}

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			// note:
			//   can overflow without exception, so the overflow check
			//   use full range of ulong by checking for 0 instead of long.min
			var value = unchecked((ulong)Interlocked.Increment(ref i));
			if (value == 0)
			{
				throw new OverflowException($"{nameof(i)} overflow!");
			}

			return base.SendAsync(request, cancellationToken);
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
					timer?.Stop();
					try
					{
						MeasureThroughput();
					}
					catch
					{
						// swallow
					}

					timer?.Dispose();

					disposed = true;
				}
			}
		}
	}

	public interface IThroughputMeasuringHandlerSettings
	{
		int IntervalInSeconds { get; }
		IThroughputProcessor ThroughputProcessor { get; }
	}

	public interface IThroughputProcessor
	{
		void Process(ulong throughput);
	}

	public class ThroughputMeasuringHandlerSettings : IThroughputMeasuringHandlerSettings
	{
		public int IntervalInSeconds { get; init; }
		public IThroughputProcessor ThroughputProcessor { get; init; }
	}
}
