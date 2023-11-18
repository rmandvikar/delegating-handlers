using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class FaultWindowSignalingHandlerTests
{
	[Test]
	public async Task Signals_Fault()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var triggerProperty = fixture.Create<string>();
		var faultWindowSignalingHandler = new FaultWindowSignalingHandler(
			new FaultWindowSignalingHandlerSettings
			{
				ProbabilityPercentage = 100d,
				FaultDuration = TimeSpan.FromMilliseconds(10),
				SignalProperty = triggerProperty,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), faultWindowSignalingHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

#pragma warning disable CS0618 // Type or member is obsolete
		Assert.IsTrue(requestMessage.Properties.TryGetValue(triggerProperty, out var valueObj) && valueObj is bool value && value);
#pragma warning restore CS0618 // Type or member is obsolete
	}

	[Test]
	public async Task Does_Not_Signal_Fault()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var triggerProperty = fixture.Create<string>();
		var faultWindowSignalingHandler = new FaultWindowSignalingHandler(
			new FaultWindowSignalingHandlerSettings
			{
				ProbabilityPercentage = 0d,
				FaultDuration = TimeSpan.FromMilliseconds(10),
				SignalProperty = triggerProperty,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), faultWindowSignalingHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

#pragma warning disable CS0618 // Type or member is obsolete
		Assert.IsFalse(requestMessage.Properties.ContainsKey(triggerProperty));
#pragma warning restore CS0618 // Type or member is obsolete
	}
}
