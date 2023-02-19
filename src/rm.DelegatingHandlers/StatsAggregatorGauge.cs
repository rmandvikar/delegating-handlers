using System;

namespace rm.DelegatingHandlers;

/// <summary>
/// Emits last.
/// </summary>
public interface IGaugeMetricEmitter
{
	void Emit(
		string metricName,
		double last);
}

public class GaugeStatsAggregator : StatsAggregatorBase
{
	private readonly IStatsAggregatorSettings statsAggregatorSettings;
	private readonly IGaugeMetricEmitter gaugeMetricEmitter;
	private double? gauge = null;

	public GaugeStatsAggregator(
		IStatsAggregatorSettings statsAggregatorSettings,
		IGaugeMetricEmitter gaugeMetricEmitter,
		Action<Exception> onProcessingError)
		: base(
			  statsAggregatorSettings,
			  onProcessingError)
	{
		this.statsAggregatorSettings = statsAggregatorSettings
			?? throw new ArgumentNullException(nameof(statsAggregatorSettings));
		this.gaugeMetricEmitter = gaugeMetricEmitter
			?? throw new ArgumentNullException(nameof(gaugeMetricEmitter));
	}

	protected override void AggregateStats()
	{
		double? gaugeForInterval;
		lock (locker)
		{
			gaugeForInterval = gauge;
			gauge = null;
		}
		if (gaugeForInterval != null)
		{
			gaugeMetricEmitter.Emit(statsAggregatorSettings.MetricName, gaugeForInterval.Value);
		}
	}

	public override void Add(double value)
	{
		lock (locker)
		{
			gauge = value;
		}
	}
}
