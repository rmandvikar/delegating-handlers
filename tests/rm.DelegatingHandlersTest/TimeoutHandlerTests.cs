using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class TimeoutHandlerTests
{
	[Test]
	public void Times_Out()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var delayHandler = new ProcrastinatingHandler(
			new ProcrastinatingHandlerSettings
			{
				DelayInMilliseconds = 1_000,
			});
		var timeoutHandler = new TimeoutHandler(
			new TimeoutHandlerSettings
			{
				TimeoutInMilliseconds = 1,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), timeoutHandler, delayHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		var ex = Assert.ThrowsAsync<TimeoutExpiredException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
	}

	[Test]
	public void Does_Not_Time_Out()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var timeoutHandler = new TimeoutHandler(
			new TimeoutHandlerSettings
			{
				TimeoutInMilliseconds = 1,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), timeoutHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		Assert.DoesNotThrowAsync(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
	}

	[Test]
	public void Does_Not_Time_Out_When_Other_Token_Cancels()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var delayHandler = new ProcrastinatingHandler(
			new ProcrastinatingHandlerSettings
			{
				DelayInMilliseconds = 100,
			});
		var timeoutHandler = new TimeoutHandler(
			new TimeoutHandlerSettings
			{
				TimeoutInMilliseconds = 1_000,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), timeoutHandler, delayHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var cts = new CancellationTokenSource(millisecondsDelay: 1);

		var ex = Assert.ThrowsAsync<TaskCanceledException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, cts.Token);
		});
		// explicit to clarify
		Assert.AreNotEqual(typeof(TimeoutExpiredException), ex!.GetType());
	}

	[Test]
	public void Is_A_TaskCanceledException()
	{
		var ex = new TimeoutExpiredException();
		Assert.IsTrue(ex is TaskCanceledException);
	}
}
