using System.Net;
using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class MetricAggregatingHandlerTests
{
	private const string AggregateStatsMethodName = "AggregateStats";

	[Test]
	public async Task Verify()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var intervalInSeconds = 10;
		fixture.Register<IStatsAggregatorSettings>(() =>
			new StatsAggregatorSettings
			{
				MetricName = "dep_metric_ms",
				IntervalInSeconds = intervalInSeconds,
			});
		fixture.Register<IStatsCalculator>(() => fixture.Create<StatsCalculator>());
		fixture.Register<Action<Exception>>(() => Console.WriteLine);
		var statsAggregatorBaseMock = fixture.Create<Mock<StatsAggregatorBase>>();
		fixture.Register<IStatsAggregator>(() => statsAggregatorBaseMock.Object);
		var metricAggregatingHandler = fixture.Build<MetricAggregatingHandler>()
			.Without(x => x.InnerHandler)
			.Create();
		using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(http200);
		using var invoker = HttpMessageInvokerFactory.Create(
			metricAggregatingHandler, shortCircuitingCannedResponseHandler);

		using var request = new HttpRequestMessage();
		var _ = await invoker.SendAsync(request, CancellationToken.None);
		invoker.Dispose();

		statsAggregatorBaseMock.Protected().Verify(AggregateStatsMethodName,
			Times.AtLeastOnce());
	}

	[Explicit]
	[Test]
	public async Task Aggregates_Histogram()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var processedCount = 0;
		Action<string, int, double, double, double, double, double, double, double, double, double, double> emit = (metricName, N, sum, avg, p0, p50, p90, p95, p99, p999, p9999, p100) =>
		{
			processedCount++;
			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] metricName: {metricName}, N: {N,10}, count (sum): {sum,10:F3}, avg: {avg,10:F3}, p0: {p0,10:F3}, p50: {p50,10:F3}, p90: {p90,10:F3}, p95: {p95,10:F3}, p99: {p99,10:F3}, p999: {p999,10:F3}, p9999: {p9999,10:F3}, p100: {p100,10:F3}");
		};
		var histogramMetricEmitterMock = fixture.Freeze<Mock<IHistogramMetricEmitter>>();
		histogramMetricEmitterMock
			.Setup(x => x.Emit(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
			.Callback(emit);
		var intervalInSeconds = 1;
		fixture.Register<IStatsAggregatorSettings>(() =>
			new StatsAggregatorSettings
			{
				MetricName = "dep_histogram_ms",
				IntervalInSeconds = intervalInSeconds,
			});
		fixture.Register<IStatsCalculator>(() => fixture.Create<StatsCalculator>());
		fixture.Register<Action<Exception>>(() => Console.WriteLine);
		fixture.Register<IStatsAggregator>(() => fixture.Create<HistogramStatsAggregator>());
		var metricAggregatingHandler = fixture.Build<MetricAggregatingHandler>()
			.Without(x => x.InnerHandler)
			.Create();
		using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(http200);
		var procrastinatingHandler = new ProcrastinatingHandler(
			new ProcrastinatingHandlerSettings
			{
				DelayInMilliseconds = 1,
			});
		using var invoker = HttpMessageInvokerFactory.Create(
			metricAggregatingHandler, procrastinatingHandler, shortCircuitingCannedResponseHandler);

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
	public async Task Aggregates_Count()
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
		fixture.Register<IStatsCalculator>(() => fixture.Create<StatsCalculator>());
		fixture.Register<Action<Exception>>(() => Console.WriteLine);
		fixture.Register<IStatsAggregator>(() => fixture.Create<CountStatsAggregator>());
		var metricAggregatingHandler = fixture.Build<MetricAggregatingHandler>()
			.Without(x => x.InnerHandler)
			.Create();
		using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(http200);
		var procrastinatingHandler = new ProcrastinatingHandler(
			new ProcrastinatingHandlerSettings
			{
				DelayInMilliseconds = 1,
			});
		using var invoker = HttpMessageInvokerFactory.Create(
			metricAggregatingHandler, procrastinatingHandler, shortCircuitingCannedResponseHandler);

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
	public async Task Aggregates_Rate()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var processedCount = 0;
		Action<string, double> emit = (metricName, rate) =>
		{
			processedCount++;
			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] metricName: {metricName}, rate: {rate,10:F3}");
		};
		var rateMetricEmitterMock = fixture.Freeze<Mock<IRateMetricEmitter>>();
		rateMetricEmitterMock
			.Setup(x => x.Emit(It.IsAny<string>(), It.IsAny<double>()))
			.Callback(emit);
		var intervalInSeconds = 1;
		fixture.Register<IStatsAggregatorSettings>(() =>
			new StatsAggregatorSettings
			{
				MetricName = "dep_rate",
				IntervalInSeconds = intervalInSeconds,
			});
		fixture.Register<IStatsCalculator>(() => fixture.Create<StatsCalculator>());
		fixture.Register<Action<Exception>>(() => Console.WriteLine);
		fixture.Register<IStatsAggregator>(() => fixture.Create<RateStatsAggregator>());
		var metricAggregatingHandler = fixture.Build<MetricAggregatingHandler>()
			.Without(x => x.InnerHandler)
			.Create();
		using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(http200);
		var procrastinatingHandler = new ProcrastinatingHandler(
			new ProcrastinatingHandlerSettings
			{
				DelayInMilliseconds = 1,
			});
		using var invoker = HttpMessageInvokerFactory.Create(
			metricAggregatingHandler, procrastinatingHandler, shortCircuitingCannedResponseHandler);

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
	public async Task Aggregates_Gauge()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var processedCount = 0;
		Action<string, double> emit = (metricName, gauge) =>
		{
			processedCount++;
			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] metricName: {metricName}, gauge: {gauge,10:F3}");
		};
		var gaugeMetricEmitterMock = fixture.Freeze<Mock<IGaugeMetricEmitter>>();
		gaugeMetricEmitterMock
			.Setup(x => x.Emit(It.IsAny<string>(), It.IsAny<double>()))
			.Callback(emit);
		var intervalInSeconds = 1;
		fixture.Register<IStatsAggregatorSettings>(() =>
			new StatsAggregatorSettings
			{
				MetricName = "dep_gauge",
				IntervalInSeconds = intervalInSeconds,
			});
		fixture.Register<IStatsCalculator>(() => fixture.Create<StatsCalculator>());
		fixture.Register<Action<Exception>>(() => Console.WriteLine);
		fixture.Register<IStatsAggregator>(() => fixture.Create<GaugeStatsAggregator>());
		var metricAggregatingHandler = fixture.Build<MetricAggregatingHandler>()
			.Without(x => x.InnerHandler)
			.Create();
		using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(http200);
		var procrastinatingHandler = new ProcrastinatingHandler(
			new ProcrastinatingHandlerSettings
			{
				DelayInMilliseconds = 1,
			});
		using var invoker = HttpMessageInvokerFactory.Create(
			metricAggregatingHandler, procrastinatingHandler, shortCircuitingCannedResponseHandler);

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

		var intervalInSeconds = 1;
		fixture.Register<IStatsAggregatorSettings>(() =>
			new StatsAggregatorSettings
			{
				MetricName = "dep_metric_ms",
				IntervalInSeconds = intervalInSeconds,
			});
		var processedCount = 0;
		var statsAggregatorBaseMock = fixture.Create<Mock<StatsAggregatorBase>>();
		var statsAggregatorBase = statsAggregatorBaseMock.Object;
		statsAggregatorBaseMock.Protected()
			.Setup(AggregateStatsMethodName)
			.Callback(() =>
			{
				processedCount++;
				Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] aggregate");
			});
		fixture.Register<IStatsAggregator>(() => statsAggregatorBase);
		fixture.Register<IStatsCalculator>(() => fixture.Create<StatsCalculator>());
		fixture.Register<Action<Exception>>(() => Console.WriteLine);
		var metricAggregatingHandler = fixture.Build<MetricAggregatingHandler>()
			.Without(x => x.InnerHandler)
			.Create();
		using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(http200);
		var procrastinatingHandler = new ProcrastinatingHandler(
			new ProcrastinatingHandlerSettings
			{
				DelayInMilliseconds = 1,
			});
		using var invoker = HttpMessageInvokerFactory.Create(
			metricAggregatingHandler, procrastinatingHandler, shortCircuitingCannedResponseHandler);

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
		statsAggregatorBase.Stop();
		Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] stop");
		await Task.Delay(TimeSpan.FromSeconds(durationInSeconds));
		Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}]      total: {i,10}");
		Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] disposing... ");
		invoker.Dispose();
		Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] processed#: {processedCount}");
		Assert.GreaterOrEqual(processedCount, durationInSeconds / intervalInSeconds - 1);
	}

	[Test]
	public async Task Verify_OnError()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var onErrorCount = 0;
		Action<Exception> onError = (ex) =>
		{
			onErrorCount++;
		};
		fixture.Register<Action<Exception>>(() => onError);
		var intervalInSeconds = 1;
		fixture.Register<IStatsAggregatorSettings>(() =>
			new StatsAggregatorSettings
			{
				MetricName = "dep_metric_ms",
				IntervalInSeconds = intervalInSeconds,
			});
		fixture.Register<IStatsCalculator>(() => fixture.Create<StatsCalculator>());
		var statsAggregatorBaseMock = fixture.Create<Mock<StatsAggregatorBase>>();
		statsAggregatorBaseMock.Protected()
			.Setup(AggregateStatsMethodName)
			.Callback(() => throw new Exception("boom!"));
		fixture.Register<IStatsAggregator>(() => statsAggregatorBaseMock.Object);
		var metricAggregatingHandler = fixture.Build<MetricAggregatingHandler>()
			.Without(x => x.InnerHandler)
			.Create();
		using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(http200);
		using var invoker = HttpMessageInvokerFactory.Create(
			metricAggregatingHandler, shortCircuitingCannedResponseHandler);

		using var request = new HttpRequestMessage();
		var _ = await invoker.SendAsync(request, CancellationToken.None);
		invoker.Dispose();

		Assert.AreEqual(1, onErrorCount);
	}

	[Test]
	public async Task Verify_OnError_If_Throws()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		Action<Exception> onError = (ex) =>
		{
			throw new Exception("boom!");
		};
		fixture.Register<Action<Exception>>(() => onError);
		var intervalInSeconds = 1;
		fixture.Register<IStatsAggregatorSettings>(() =>
			new StatsAggregatorSettings
			{
				MetricName = "dep_metric_ms",
				IntervalInSeconds = intervalInSeconds,
			});
		fixture.Register<IStatsCalculator>(() => fixture.Create<StatsCalculator>());
		var statsAggregatorBaseMock = fixture.Create<Mock<StatsAggregatorBase>>();
		statsAggregatorBaseMock.Protected()
			.Setup(AggregateStatsMethodName)
			.Callback(() => throw new Exception("boom!"));
		fixture.Register<IStatsAggregator>(() => statsAggregatorBaseMock.Object);
		var metricAggregatingHandler = fixture.Build<MetricAggregatingHandler>()
			.Without(x => x.InnerHandler)
			.Create();
		using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(http200);
		using var invoker = HttpMessageInvokerFactory.Create(
			metricAggregatingHandler, shortCircuitingCannedResponseHandler);

		using var request = new HttpRequestMessage();
		var _ = await invoker.SendAsync(request, CancellationToken.None);
		invoker.Dispose();
	}
}
