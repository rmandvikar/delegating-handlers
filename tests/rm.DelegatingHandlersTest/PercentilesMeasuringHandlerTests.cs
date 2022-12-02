using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class PercentilesMeasuringHandlerTests
	{
		[Explicit]
		[Test]
		public async Task Measures_Percentiles()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var processedCount = 0;
			Action<string, int, double, double, double, double, double, double> processPercentiles = (metricName, N, p50, p90, p95, p99, p999, p9999) =>
			{
				processedCount++;
				Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] metricName: {metricName}, N: {N,10}, p50: {p50,10:F3}, p90: {p90,10:F3}, p95: {p95,10:F3}, p99: {p99,10:F3}, p999: {p999,10:F3}, p9999: {p9999,10:F3}");
			};
			var percentilesMeasuringProcessorMock = fixture.Freeze<Mock<IPercentilesMeasuringProcessor>>();
			percentilesMeasuringProcessorMock
				.Setup(x => x.Process(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
				.Callback(processPercentiles);
			var intervalInSeconds = 1;
			var percentilesMeasuringHandler =
				new PercentilesMeasuringHandler(
					new PercentilesMeasuringHandlerSettings
					{
						MetricName = "dep_latency_ms",
						IntervalInSeconds = intervalInSeconds,
						PercentilesMeasuringProcessor = percentilesMeasuringProcessorMock.Object,
					});
			using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
			var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(http200);
			var procrastinatingHandler = new ProcrastinatingHandler(
				new ProcrastinatingHandlerSettings
				{
					DelayInMilliseconds = 1,
				});
			using var invoker = HttpMessageInvokerFactory.Create(
				percentilesMeasuringHandler, procrastinatingHandler, shortCircuitingCannedResponseHandler);

			var durationInSeconds = 20;
			var endTime = DateTime.Now.AddSeconds(durationInSeconds);
			var i = 0;
			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] start");
			var tasksCount = 5;
			var tasks = new List<Task<HttpResponseMessage>>(tasksCount);
			while (DateTime.Now < endTime)
			{
				using var request = new HttpRequestMessage();
				var task = invoker.SendAsync(request, CancellationToken.None);
				tasks.Add(task);
				checked
				{
					i++;
				}
				try
				{
					await Task.WhenAll(tasks);
				}
				catch
				{
					// swallow
				}
				finally
				{
					tasks.Clear();
				}
			}
			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}]      total: {i,10}");
			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] disposing... ");
			invoker.Dispose();
			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] processed#: {processedCount}");
			Assert.GreaterOrEqual(processedCount, durationInSeconds / intervalInSeconds - 1);
#if DEBUG
			if (percentilesMeasuringHandler.exceptions.Any())
			{
				Console.WriteLine(percentilesMeasuringHandler.exceptions.Count);
				foreach (var ex in percentilesMeasuringHandler.exceptions)
				{
					Console.WriteLine(ex);
				}
			}
#endif
		}
	}
}
