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
		[Test]
		public async Task Verify()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var metricEmitterMock = fixture.Freeze<Mock<IMetricEmitter>>();
			var intervalInSeconds = 10;
			var percentilesMeasuringHandler =
				new PercentilesMeasuringHandler(
					new PercentilesMeasuringProcessor(
						new PercentilesMeasuringProcessorSettings
						{
							MetricName = "dep_latency_ms",
							IntervalInSeconds = intervalInSeconds,
						},
						metricEmitterMock.Object,
						(ex) => Console.WriteLine(ex)
						)
					);
			using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
			var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(http200);
			using var invoker = HttpMessageInvokerFactory.Create(
				percentilesMeasuringHandler, shortCircuitingCannedResponseHandler);

			using var request = new HttpRequestMessage();
			var _ = await invoker.SendAsync(request, CancellationToken.None);
			invoker.Dispose();

			metricEmitterMock.Verify(x =>
				x.Emit(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()),
				Times.Once);
		}

		[Explicit]
		[Test]
		public async Task Measures_Percentiles()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var processedCount = 0;
			Action<string, int, double, double, double, double, double, double, double, double> emitPercentiles = (metricName, N, p0, p50, p90, p95, p99, p999, p9999, p100) =>
			{
				processedCount++;
				Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] metricName: {metricName}, N: {N,10}, p0: {p0,10:F3}, p50: {p50,10:F3}, p90: {p90,10:F3}, p95: {p95,10:F3}, p99: {p99,10:F3}, p999: {p999,10:F3}, p9999: {p9999,10:F3}, p100: {p100,10:F3}");
			};
			var metricEmitterMock = fixture.Freeze<Mock<IMetricEmitter>>();
			metricEmitterMock
				.Setup(x => x.Emit(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
				.Callback(emitPercentiles);
			var intervalInSeconds = 1;
			var percentilesMeasuringHandler =
				new PercentilesMeasuringHandler(
					new PercentilesMeasuringProcessor(
						new PercentilesMeasuringProcessorSettings
						{
							MetricName = "dep_latency_ms",
							IntervalInSeconds = intervalInSeconds,
						},
						metricEmitterMock.Object,
						(ex) => Console.WriteLine(ex)
						)
					);
			using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
			var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(http200);
			var procrastinatingHandler = new ProcrastinatingHandler(
				new ProcrastinatingHandlerSettings
				{
					DelayInMilliseconds = 1,
				});
			using var invoker = HttpMessageInvokerFactory.Create(
				percentilesMeasuringHandler, procrastinatingHandler, shortCircuitingCannedResponseHandler);

			var durationInSeconds = 10;
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
		}

		[Explicit]
		[Test]
		public async Task Starts_Stops()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var processedCount = 0;
			Action<string, int, double, double, double, double, double, double, double, double> emitPercentiles = (metricName, N, p0, p50, p90, p95, p99, p999, p9999, p100) =>
			{
				processedCount++;
				Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] metricName: {metricName}, N: {N,10}, p0: {p0,10:F3}, p50: {p50,10:F3}, p90: {p90,10:F3}, p95: {p95,10:F3}, p99: {p99,10:F3}, p999: {p999,10:F3}, p9999: {p9999,10:F3}, p100: {p100,10:F3}");
			};
			var metricEmitterMock = fixture.Freeze<Mock<IMetricEmitter>>();
			metricEmitterMock
				.Setup(x => x.Emit(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
				.Callback(emitPercentiles);
			var intervalInSeconds = 1;
			var percentilesMeasuringProcessor =
				new PercentilesMeasuringProcessor(
					new PercentilesMeasuringProcessorSettings
					{
						MetricName = "dep_latency_ms",
						IntervalInSeconds = intervalInSeconds,
					},
					metricEmitterMock.Object,
					(ex) => Console.WriteLine(ex)
					);
			var percentilesMeasuringHandler =
				new PercentilesMeasuringHandler(percentilesMeasuringProcessor);
			using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
			var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(http200);
			var procrastinatingHandler = new ProcrastinatingHandler(
				new ProcrastinatingHandlerSettings
				{
					DelayInMilliseconds = 1,
				});
			using var invoker = HttpMessageInvokerFactory.Create(
				percentilesMeasuringHandler, procrastinatingHandler, shortCircuitingCannedResponseHandler);

			var durationInSeconds = 2;
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
			percentilesMeasuringProcessor.Stop();
			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] stop");
			await Task.Delay(durationInSeconds * 1000);
			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}]      total: {i,10}");
			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] disposing... ");
			invoker.Dispose();
			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] processed#: {processedCount}");
			Assert.GreaterOrEqual(processedCount, durationInSeconds / intervalInSeconds - 1);
		}
	}
}
