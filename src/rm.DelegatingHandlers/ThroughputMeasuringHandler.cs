using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Measures throughput for given interval.
/// </summary>
/// <remarks>
/// Interval range is [1, 86,400] in seconds.
/// </remarks>
public class ThroughputMeasuringHandler : DelegatingHandler
{
	private readonly CountStatsAggregator countStatsAggregator;

	/// <inheritdoc cref="ThroughputMeasuringHandler" />
	public ThroughputMeasuringHandler(
		CountStatsAggregator countStatsAggregator)
	{
		this.countStatsAggregator = countStatsAggregator
			?? throw new ArgumentNullException(nameof(countStatsAggregator));

		this.countStatsAggregator.Start();
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		try
		{
			return await base.SendAsync(request, cancellationToken)
				.ConfigureAwait(false);
		}
		finally
		{
			// note: measure AFTER request is processed
			countStatsAggregator.Add(1);
		}
	}

	private bool disposed = false;

	protected override void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				countStatsAggregator?.Dispose();

				disposed = true;
			}
		}

		base.Dispose(disposing);
	}
}
