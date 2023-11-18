using System.Net;
using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ThroughputMeasuringHandlerTests
{
	[Explicit]
	[Test]
	public async Task Measures_Throughput()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var processedCount = 0;
		Action<string, double> emit = (metricName, count) =>
		{
			processedCount++;
			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] metricName: {metricName}, count: {count,10}");
		};
		var countMetricEmitterMock = fixture.Freeze<Mock<ICountMetricEmitter>>();
		countMetricEmitterMock
			.Setup(x => x.Emit(It.IsAny<string>(), It.IsAny<double>()))
			.Callback(emit);
		var intervalInSeconds = 1;
		fixture.Register<IStatsAggregatorSettings>(() =>
			new StatsAggregatorSettings
			{
				MetricName = "dep_count",
				IntervalInSeconds = intervalInSeconds,
			});
		fixture.Register<Action<Exception>>(() => Console.WriteLine);
		fixture.Register<IStatsAggregator>(() => fixture.Create<CountStatsAggregator>());
		var throughputMeasuringHandler = fixture.Build<ThroughputMeasuringHandler>()
			.Without(x => x.InnerHandler)
			.Create();
		using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(http200);
		using var invoker = HttpMessageInvokerFactory.Create(
			throughputMeasuringHandler, shortCircuitingCannedResponseHandler);

		var durationInSeconds = 5;
		var endTime = DateTime.Now.AddSeconds(durationInSeconds);
		var i = 0;
		Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] start");
		var tasksCount = 5;
		var tasks = new List<Task<HttpResponseMessage>>(tasksCount);
		while (DateTime.Now < endTime)
		{
			using var request = new HttpRequestMessage();
			using var _ = await invoker.SendAsync(request, CancellationToken.None);
			checked
			{
				i++;
			}
		}
		Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}]      total: {i,10}");
		Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] disposing... ");
		invoker.Dispose();
		Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] processed#: {processedCount}");
		Assert.GreaterOrEqual(processedCount, durationInSeconds / intervalInSeconds - 1);
	}
}
