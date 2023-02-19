using System;
using System.Collections.Generic;
using System.Linq;

namespace rm.DelegatingHandlers;

/// <summary>
/// Helper methods for stats.
/// </summary>
public interface IStatsCalculator
{
	double Sum(IList<double> sequence);

	double Average(IList<double> sequence);

	double Last(IList<double> sequence);

	/// <summary>
	/// Calculates percentiles for given sorted <paramref name="sequence"/>.
	/// <para/>
	/// <remarks>
	/// <paramref name="sequence"/> is sorted. count > 0.
	/// </remarks>
	/// <para/>
	/// </summary>
	double Percentile(IList<double> sequence, double percentile);
}

/// <inheritdoc cref="IStatsCalculator"/>
public class StatsCalculator : IStatsCalculator
{
	public double Sum(IList<double> sequence)
	{
		var N = sequence.Count;
		if (N <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(N), $"N > 0. N: {N}");
		}
		// throws ex on overflow
		return sequence.Sum();
	}

	public double Average(IList<double> sequence)
	{
		var N = sequence.Count;
		if (N <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(N), $"N > 0. N: {N}");
		}
		return sequence.Average();
	}

	public double Last(IList<double> sequence)
	{
		var N = sequence.Count;
		if (N <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(N), $"N > 0. N: {N}");
		}
		return sequence.Last();
	}

	/// <summary>
	/// <inheritdoc/>
	/// <see href="https://stackoverflow.com/questions/8137391/percentile-calculation">source</see>
	/// </summary>
	public double Percentile(IList<double> sequence, double percentile)
	{
		var N = sequence.Count;
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
}
