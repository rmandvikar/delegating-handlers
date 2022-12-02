using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Aggregates metric.
	/// </summary>
	public class MetricAggregatingHandler : DelegatingHandler
	{
		private readonly IStatsAggregator statsAggregator;

		/// <inheritdoc cref="MetricAggregatingHandler" />
		public MetricAggregatingHandler(
			IStatsAggregator statsAggregator)
		{
			this.statsAggregator = statsAggregator
				?? throw new ArgumentNullException(nameof(statsAggregator));

			this.statsAggregator.Start();
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
				// note: aggregate AFTER request is processed
				stopwatch.Stop();
				try
				{
					statsAggregator.Add(stopwatch.ElapsedMilliseconds);
				}
				catch
				{
					// swallow
				}
			}
		}

		private bool disposed = false;

		protected override void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					statsAggregator?.Dispose();

					disposed = true;
				}
			}

			base.Dispose(disposing);
		}
	}
}
