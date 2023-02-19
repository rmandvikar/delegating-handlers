using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class StatsAggregatorTests
{
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
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		fixture.Register<IStatsAggregatorSettings>(() =>
			new StatsAggregatorSettings
			{
				MetricName = "dep_metric",
				IntervalInSeconds = intervalInSeconds, // to bypass validation
			});
		var statsAggregator = fixture.Create<CountStatsAggregator>();
		var intervalDelay = statsAggregator.CalculateNextIntervalDelay(intervalInSeconds);
		var now = DateTime.Now;
		var ts = now.AddMilliseconds(intervalDelay);
		Console.WriteLine($"now:{now:o}");
		Console.WriteLine($" ts:{ts:o}");
	}
}
