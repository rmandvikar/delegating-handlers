using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ScenarioTests
{
	[Test]
	public void Retry_To_Fix_Infrequent_TaskCanceledException_Using_HttpMessageInvoker()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var procrastinatingHandler = new ProcrastinatingHandler(
			new ProcrastinatingHandlerSettings
			{
				DelayInMilliseconds = 1_000,
			});
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var timeoutHandler = new TimeoutHandler(
			new TimeoutHandlerSettings
			{
				TimeoutInMilliseconds = 10,
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 1,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, timeoutHandler, delegateHandler, procrastinatingHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		var ex = Assert.ThrowsAsync<TimeoutExpiredException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
		Assert.AreEqual(typeof(TaskCanceledException), ex!.InnerException!.GetType());
		Assert.AreEqual(1, retryAttempt);
	}

#if NET6_0 // test fails in NET7_0, so not NET6_0_OR_GREATER
	/// <remarks>
	/// HttpClient massages the ex thrown (at least on net6.0), so this test.
	/// </remarks>
	[Test]
	public void Retry_To_Fix_Infrequent_TaskCanceledException()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var procrastinatingHandler = new ProcrastinatingHandler(
			new ProcrastinatingHandlerSettings
			{
				DelayInMilliseconds = 1_000,
			});
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var timeoutHandler = new TimeoutHandler(
			new TimeoutHandlerSettings
			{
				TimeoutInMilliseconds = 10,
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 1,
				RetryDelayInMilliseconds = 0,
			});

		using var httpClient = HttpClientFactory.Create(
			retryHandler, timeoutHandler, delegateHandler, procrastinatingHandler);
		httpClient.Timeout = TimeSpan.FromMilliseconds(30_000);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		var ex = Assert.ThrowsAsync<TaskCanceledException>(async () =>
		{
			using var _ = await httpClient.SendAsync(requestMessage, CancellationToken.None);
		});
		Exception penultimateInnerEx = null!;
		Exception lastInnerEx = ex!;
		while (lastInnerEx.InnerException != null)
		{
			penultimateInnerEx = lastInnerEx;
			lastInnerEx = lastInnerEx.InnerException;
		}
		Assert.AreEqual(typeof(TimeoutExpiredException), penultimateInnerEx.GetType());
		Assert.AreEqual(typeof(TaskCanceledException), lastInnerEx.GetType());
		Assert.AreEqual(1, retryAttempt);
	}
#endif

	[Test]
	public void Retry_With_Higher_Timeout_Does_Not_Throw_TimeoutExpiredException()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var procrastinatingHandler = new ProcrastinatingHandler(
			new ProcrastinatingHandlerSettings
			{
				DelayInMilliseconds = 1_000,
			});
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var timeoutHandler = new TimeoutHandler(
			new TimeoutHandlerSettings
			{
				TimeoutInMilliseconds = 10_000,
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 1,
				RetryDelayInMilliseconds = 0,
			});

		using var httpClient = HttpClientFactory.Create(
			retryHandler, timeoutHandler, delegateHandler, procrastinatingHandler);
		httpClient.Timeout = TimeSpan.FromMilliseconds(10);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		var ex = Assert.ThrowsAsync<TaskCanceledException>(async () =>
		{
			using var _ = await httpClient.SendAsync(requestMessage, CancellationToken.None);
		});
		Assert.AreEqual(0, retryAttempt);
	}

	[Test]
	public void Cause_Fault_Window()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var throwingOnConditionHandler = new ThrowingOnConditionHandler(new TurnDownForWhatException());
		var faultWindowSignalingHandler = new FaultWindowSignalingHandler(
			new FaultWindowSignalingHandlerSettings
			{
				ProbabilityPercentage = 100d,
				FaultDuration = TimeSpan.FromMilliseconds(10),
				SignalProperty = typeof(ThrowingOnConditionHandler).FullName
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			faultWindowSignalingHandler, throwingOnConditionHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		Assert.ThrowsAsync<TurnDownForWhatException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
	}

	[Test]
	public void Cause_Fault_Window_On_Condition()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var throwingOnConditionHandler = new ThrowingOnConditionHandler(new TurnDownForWhatException());
		var faultWindowSignalingOnConditionHandler = new FaultWindowSignalingOnConditionHandler(
			new FaultWindowSignalingOnConditionHandlerSettings
			{
				ProbabilityPercentage = 100d,
				FaultDuration = TimeSpan.FromMilliseconds(10),
				SignalProperty = typeof(ThrowingOnConditionHandler).FullName
			});
		var faultOnConditionHandler = new FaultOnConditionHandler();

		using var invoker = HttpMessageInvokerFactory.Create(
			faultOnConditionHandler, faultWindowSignalingOnConditionHandler, throwingOnConditionHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		Assert.ThrowsAsync<TurnDownForWhatException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
	}
}
