using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class ThroughputMeasuringHandlerTests
	{
		[Explicit]
		[Test]
		public async Task Measures_Throughput()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			Action<ulong> processThroughput = (throughput) =>
				Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] throughput: {throughput,10}");
			var throughputProcessorMock = fixture.Freeze<Mock<IThroughputProcessor>>();
			throughputProcessorMock.Setup(x => x.Process(It.IsAny<ulong>())).Callback(processThroughput);
			var throughputMeasuringHandler =
				new ThroughputMeasuringHandler(
					new ThroughputMeasuringHandlerSettings
					{
						IntervalInSeconds = 1,
						ThroughputProcessor = throughputProcessorMock.Object,
					});
			var shortCircuitingCannedResponseHandler =
				new ShortCircuitingCannedResponseHandler(new HttpResponseMessage(HttpStatusCode.OK));
			using var invoker = HttpMessageInvokerFactory.Create(
				throughputMeasuringHandler, shortCircuitingCannedResponseHandler);

			var durationInSeconds = 10;
			var endTime = DateTime.Now.AddSeconds(durationInSeconds);
			var i = (ulong)0;
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
		}

		[Test]
		[TestCase(1)]      // 1s
		[TestCase(10)]     // 10s
		[TestCase(60)]     // 1m
		[TestCase(600)]    // 10m
		[TestCase(900)]    // 15m
		[TestCase(1_800)]  // 30m
		[TestCase(3_600)]  // 1h
		[TestCase(21_600)] // 6h
		[TestCase(43_200)] // 12h
		[TestCase(86_400)] // 1d
		public void Verify_Interval(int intervalInSeconds)
		{
			var throughputMeasuringHandler = new ThroughputMeasuringHandler(
				new ThroughputMeasuringHandlerSettings
				{
					IntervalInSeconds = intervalInSeconds,
					ThroughputProcessor = null,
				});
			var intervalDelay = throughputMeasuringHandler.CalculateNextIntervalDelay(intervalInSeconds);
			var now = DateTime.Now;
			var ts = now.AddMilliseconds(intervalDelay);
			Console.WriteLine($"now:{now:o}");
			Console.WriteLine($" ts:{ts:o}");
		}

		[Explicit]
		[Test]
		public async Task Measures_Throughput_Even_When_Zero()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			Action<ulong> processThroughput = (throughput) =>
				Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] throughput: {throughput}");
			var throughputProcessorMock = fixture.Freeze<Mock<IThroughputProcessor>>();
			throughputProcessorMock.Setup(x => x.Process(It.IsAny<ulong>())).Callback(processThroughput);
			var throughputMeasuringHandler =
				new ThroughputMeasuringHandler(
					new ThroughputMeasuringHandlerSettings
					{
						IntervalInSeconds = 1,
						ThroughputProcessor = throughputProcessorMock.Object,
					});
			using var invoker = HttpMessageInvokerFactory.Create(
				throughputMeasuringHandler);

			await Task.Delay(2_000);
			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] disposing... ");
		}
	}
}
