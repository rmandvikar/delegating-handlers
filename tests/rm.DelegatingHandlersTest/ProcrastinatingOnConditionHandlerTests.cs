using System.Diagnostics;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ProcrastinatingOnConditionHandlerTests
{
	[Test]
	public async Task Procrastinates()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var delayInMilliseconds = 25;
		var procrastinatingOnConditionHandler = new ProcrastinatingOnConditionHandler(
			new ProcrastinatingOnConditionHandlerSettings
			{
				DelayInMilliseconds = delayInMilliseconds,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), procrastinatingOnConditionHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
#pragma warning disable CS0618 // Type or member is obsolete
		requestMessage.Properties[typeof(ProcrastinatingOnConditionHandler).FullName!] = true;
#pragma warning restore CS0618 // Type or member is obsolete
		var stopwatch = Stopwatch.StartNew();
		using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);
		stopwatch.Stop();
		Console.WriteLine(stopwatch.ElapsedMilliseconds);

		Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, delayInMilliseconds);
	}

	[Test]
	[TestCase(true, false)]
	[TestCase(true, null)]
	[TestCase(false, null)]
	public async Task Does_Not_Procrastinate(bool isValuePresent, object value)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var delayInMilliseconds = 1000;
		var procrastinatingOnConditionHandler = new ProcrastinatingOnConditionHandler(
			new ProcrastinatingOnConditionHandlerSettings
			{
				DelayInMilliseconds = delayInMilliseconds,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), procrastinatingOnConditionHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		if (isValuePresent)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			requestMessage.Properties[typeof(ProcrastinatingOnConditionHandler).FullName!] = value;
#pragma warning restore CS0618 // Type or member is obsolete
		}
		var stopwatch = Stopwatch.StartNew();
		using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);
		stopwatch.Stop();
		Console.WriteLine(stopwatch.ElapsedMilliseconds);

		Assert.Less(stopwatch.ElapsedMilliseconds, delayInMilliseconds);
	}

	[Test]
	[TestCase(0)]
	public void Throws_On_Invalid_Value(object value)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var delayInMilliseconds = 1000;
		var procrastinatingOnConditionHandler = new ProcrastinatingOnConditionHandler(
			new ProcrastinatingOnConditionHandlerSettings
			{
				DelayInMilliseconds = delayInMilliseconds,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), procrastinatingOnConditionHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
#pragma warning disable CS0618 // Type or member is obsolete
		requestMessage.Properties[typeof(ProcrastinatingOnConditionHandler).FullName!] = value;
#pragma warning restore CS0618 // Type or member is obsolete
		Assert.ThrowsAsync<InvalidCastException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
	}
}
