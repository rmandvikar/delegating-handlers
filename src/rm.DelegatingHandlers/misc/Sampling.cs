using System;
using System.Numerics;
using K4os.Hash.xxHash;
using Serilog.Core;
using Serilog.Events;

namespace rm.DelegatingHandlers;

/// <summary>
/// Defines sampling methods.
/// </summary>
public interface ISampler
{
	bool IsSampled(byte[] id);
}

/// <summary>
/// Fixed-rate sampler that samples by a percentage [0.00, 100.00].
/// </summary>
public class FixedRateSampler : ISampler
{
	private readonly double samplingPercentage;

	/// <inheritdoc cref="FixedRateSampler"/>
	public FixedRateSampler(
		double samplingPercentage)
	{
		this.samplingPercentage = samplingPercentage;
	}

	public bool IsSampled(byte[] id)
	{
		_ = id ?? throw new ArgumentNullException(nameof(id));
		if (samplingPercentage <= 0)
		{
			return false;
		}
		if (samplingPercentage >= 100)
		{
			return true;
		}
		var isSampled = GetPercentValue(id) <= samplingPercentage;
		return isSampled;
	}

	/// <remarks>
	/// Returns [0.01, 100.00].
	/// </remarks>
	private double GetPercentValue(byte[] id)
	{
		//return ((unchecked((uint)id.GetHashCode()) % 100_00) + 1) / 100d;
		//return ((int)(BigInteger.Abs(new BigInteger(id)) % 100_00) + 1) / 100d;
		return ((XXH64.DigestOf(id) % 100_00) + 1) / 100d;
	}
}

/// <summary>
/// LogEvent Filter with fixed-rate sampling.
/// </summary>
public class FixedRateSamplingLogEventFilter : ILogEventFilter
{
	private readonly ISamplingCriteria samplingCriteria;
	private readonly IIdFactory idFactory;
	private readonly ISampler sampler;

	private readonly LogEventProperty samplingPercentageLogEventProperty;

	public FixedRateSamplingLogEventFilter(
		ISamplingCriteria samplingCriteria,
		IIdFactory idFactory,
		double? samplingPercentage)
	{
		this.samplingCriteria = samplingCriteria
			?? throw new ArgumentNullException(nameof(samplingCriteria));
		this.idFactory = idFactory
			?? throw new ArgumentNullException(nameof(idFactory));

		// sample in by default
		var percentage = samplingPercentage ?? 100d;
		sampler = new FixedRateSampler(percentage);
		samplingPercentageLogEventProperty =
			new LogEventProperty($"{typeof(FixedRateSampler).Name}.Percentage", new ScalarValue(percentage));
	}

	public bool IsEnabled(LogEvent logEvent)
	{
		// only sample logs by criteria
		if (!samplingCriteria.Filter(logEvent))
		{
			return true;
		}

		// sample request based on an id of transaction to sample in/out all of its logs
		var id = idFactory.Get();
		// sample in by default
		if (id == null)
		{
			return true;
		}

		// enrich samplingPercentage to diag
		logEvent.AddPropertyIfAbsent(samplingPercentageLogEventProperty);

		return sampler.IsSampled(id);
	}
}

/// <summary>
/// Defines criteria to sample <see cref="LogEvent"/>s.
/// </summary>
public interface ISamplingCriteria
{
	bool Filter(LogEvent logEvent);
}

/// <summary>
/// Defines factory methods for transaction id.
/// </summary>
public interface IIdFactory
{
	/// <summary>
	/// Returns an id of transaction which is used to sample in/sample out all of its logs.
	/// </summary>
	byte[] Get();
}
