using System;
using System.Threading;
using rm.Extensions;

namespace rm.DelegatingHandlers;

/// <summary>
/// Aggregates stats.
/// </summary>
/// <note>
/// See https://stackoverflow.com/questions/14368663/determining-if-idisposable-should-extend-an-interface-or-be-implemented-on-a-cla/14368765#14368765
/// See https://stackoverflow.com/questions/10513948/declare-idisposable-for-the-class-or-interface
/// </note>
public interface IStatsAggregator : IDisposable
{
	void Start();
	void Stop();
	void Add(double value);
}

public interface IStatsAggregatorSettings
{
	string MetricName { get; }
	int IntervalInSeconds { get; }
}

public record class StatsAggregatorSettings : IStatsAggregatorSettings
{
	public string MetricName { get; init; }
	public int IntervalInSeconds { get; init; }
}

/// <summary>
/// Aggregates stats for given interval.
/// <para/>
/// Interval range is [1, 60] in seconds.
/// </summary>
public abstract class StatsAggregatorBase : IStatsAggregator
{
	private readonly IStatsAggregatorSettings statsAggregatorSettings;
	private readonly Action<Exception> onProcessingError;

	private readonly Timer timer;
	protected readonly object locker = new object();
	private bool processing;
	protected const int SecondsInDay = 86_400; // 24h * 60m * 60s
	protected const int SecondsInMinute = 60; // 1m * 60s

	public StatsAggregatorBase(
		IStatsAggregatorSettings statsAggregatorSettings,
		Action<Exception> onProcessingError)
	{
		_ = statsAggregatorSettings
			?? throw new ArgumentNullException(nameof(statsAggregatorSettings));
		if (statsAggregatorSettings.IntervalInSeconds <= 0
			|| statsAggregatorSettings.IntervalInSeconds > SecondsInDay)
		{
			throw new ArgumentOutOfRangeException(
				nameof(statsAggregatorSettings.IntervalInSeconds),
				$"0 < {statsAggregatorSettings.IntervalInSeconds} <= {SecondsInDay}");
		}
		this.statsAggregatorSettings = statsAggregatorSettings;
		this.onProcessingError = onProcessingError
			?? throw new ArgumentNullException(nameof(onProcessingError));

		timer = new Timer(AggregateStatsCallback, null, Timeout.Infinite, Timeout.Infinite);
	}

	private void AggregateStatsCallback(object state)
	{
		try
		{
			AggregateStats();
		}
		catch (Exception ex)
		{
			// swallow
			try
			{
				onProcessingError?.Invoke(ex);
			}
			catch
			{
				// swallow
			}
		}
		finally
		{
			// setup
			StartTimer();
		}
	}

	protected abstract void AggregateStats();

	private void StartTimer()
	{
		if (disposed)
		{
			return;
		}

		// bin intervals
		// recalculate due to time creep
		var nextIntervalDelayInMilliseconds =
			CalculateNextIntervalDelay(statsAggregatorSettings.IntervalInSeconds);
		// schedule
		timer.Change(nextIntervalDelayInMilliseconds, Timeout.Infinite);
		lock (locker)
		{
			processing = true;
		}
	}

	public void Start()
	{
		lock (locker)
		{
			if (processing)
			{
				return;
			}
			processing = true;
		}

		StartTimer();
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

	private void StopTimer()
	{
		if (disposed)
		{
			return;
		}

		timer.Change(Timeout.Infinite, Timeout.Infinite);
		lock (locker)
		{
			processing = false;
		}
	}

	public void Stop()
	{
		lock (locker)
		{
			if (!processing)
			{
				return;
			}
			processing = false;
		}

		StopTimer();
	}

	public abstract void Add(double value);

	private bool disposed = false;

	public void Dispose()
	{
		Dispose(true);
	}

	protected virtual void Dispose(bool disposing)
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
					AggregateStats();
				}
				catch (Exception ex)
				{
					// swallow
					onProcessingError?.Invoke(ex);
				}

				timer?.Dispose();

				disposed = true;
			}
		}
	}
}
