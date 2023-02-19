using System;

namespace rm.DelegatingHandlers;

/// <summary>
/// Emits count.
/// </summary>
public interface ICountMetricEmitter
{
	void Emit(
		string metricName,
		double count);
}

public class CountStatsAggregator : StatsAggregatorBase
{
	private readonly IStatsAggregatorSettings statsAggregatorSettings;
	private readonly ICountMetricEmitter countMetricEmitter;
	private double? count = null;

	public CountStatsAggregator(
		IStatsAggregatorSettings statsAggregatorSettings,
		ICountMetricEmitter countMetricEmitter,
		Action<Exception> onProcessingError)
		: base(
			  statsAggregatorSettings,
			  onProcessingError)
	{
		this.statsAggregatorSettings = statsAggregatorSettings
			?? throw new ArgumentNullException(nameof(statsAggregatorSettings));
		this.countMetricEmitter = countMetricEmitter
			?? throw new ArgumentNullException(nameof(countMetricEmitter));
	}

	protected override void AggregateStats()
	{
		double? countForInterval;
		lock (locker)
		{
			countForInterval = count;
			count = null;
		}
		if (countForInterval != null)
		{
			countMetricEmitter.Emit(statsAggregatorSettings.MetricName, countForInterval.Value);
		}
	}

	public override void Add(double value)
	{
		lock (locker)
		{
			count = (count ?? 0) + value;
		}
	}
}
