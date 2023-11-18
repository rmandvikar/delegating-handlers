using System.Net;
using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ExponentialBackoffWithJitterRetryOnSignalHandlerTests
{
	[Test]
	public async Task Retries_On_Signal()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var shortCircuitingCannedActionsHandler = new ShortCircuitingCannedActionsHandler(
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)404 }, // retry
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)200, Content = new StringContent("yawn!") }, // retry
			(request) => throw new TaskCanceledException("timeout!"),                    // retry
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)200 }  // NO retry
			);
		var retrySignalingOnConditionHandler = new RetrySignalingOnConditionHandler();
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryOnSignalHandler(
			new RetrySettings
			{
				RetryCount = 5,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, retrySignalingOnConditionHandler, shortCircuitingCannedActionsHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(3, retryAttempt);
	}

	[Test]
	[TestCase(0)]
	[TestCase(5)]
	public void Retries_On_Signal_All_Attempts_In_Exception(int retryCount)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var shortCircuitingCannedActionsHandler = new ShortCircuitingCannedActionsHandler(
			(request) => throw new TaskCanceledException("timeout!"), // retry
			(request) => throw new TaskCanceledException("timeout!"), // retry
			(request) => throw new TaskCanceledException("timeout!"), // retry
			(request) => throw new TaskCanceledException("timeout!"), // retry
			(request) => throw new TaskCanceledException("timeout!"), // retry
			(request) => throw new TaskCanceledException("timeout!")  // retry
			);
		var retrySignalingOnConditionHandler = new RetrySignalingOnConditionHandler();
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryOnSignalHandler(
			new RetrySettings
			{
				RetryCount = retryCount,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, retrySignalingOnConditionHandler, shortCircuitingCannedActionsHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		Assert.ThrowsAsync<TaskCanceledException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
		Assert.AreEqual(retryCount, retryAttempt);
	}

	[Test]
	public void Retries_On_Signal_Last_Attempt_In_Exception()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var shortCircuitingCannedActionsHandler = new ShortCircuitingCannedActionsHandler(
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)404 }, // retry
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)200, Content = new StringContent("yawn!") }, // retry
			(request) => throw new TaskCanceledException("timeout!"), // retry
			(request) => throw new TaskCanceledException("timeout!"), // retry
			(request) => throw new TaskCanceledException("timeout!"), // retry
			(request) => throw new TaskCanceledException("timeout!")  // last attempt
			);
		var retrySignalingOnConditionHandler = new RetrySignalingOnConditionHandler();
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryOnSignalHandler(
			new RetrySettings
			{
				RetryCount = 5,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, retrySignalingOnConditionHandler, shortCircuitingCannedActionsHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		Assert.ThrowsAsync<TaskCanceledException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
		Assert.AreEqual(5, retryAttempt);
	}

	[Test]
	[TestCase(0)]
	[TestCase(5)]
	public void No_Retries_On_Signal_When_Unhandled_In_Exception(int retryCount)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var shortCircuitingCannedActionsHandler = new ShortCircuitingCannedActionsHandler(
			(request) => throw new TurnDownForWhatException()         // unhandled
			);
		var retrySignalingOnConditionHandler = new RetrySignalingOnConditionHandler();
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryOnSignalHandler(
			new RetrySettings
			{
				RetryCount = retryCount,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, retrySignalingOnConditionHandler, shortCircuitingCannedActionsHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		Assert.ThrowsAsync<TurnDownForWhatException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
		Assert.AreEqual(0, retryAttempt);
	}

	[Test]
	public async Task Does_Not_Retry_If_No_Signal()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var shortCircuitingCannedActionsHandler = new ShortCircuitingCannedActionsHandler(
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)200 } // NO retry
			);
		var retrySignalingOnConditionHandler = new RetrySignalingOnConditionHandler();
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryOnSignalHandler(
			new RetrySettings
			{
				RetryCount = 5,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, retrySignalingOnConditionHandler, shortCircuitingCannedActionsHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(0, retryAttempt);
	}

	[Test]
	public async Task When_0_Retries_PollyRetryAttempt_Property_Is_Not_Present()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var shortCircuitingCannedActionsHandler = new ShortCircuitingCannedActionsHandler(
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)404 });
		var retrySignalingOnConditionHandler = new RetrySignalingOnConditionHandler();
		var retryHandler = new ExponentialBackoffWithJitterRetryOnSignalHandler(
			new RetrySettings
			{
				RetryCount = 0,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, retrySignalingOnConditionHandler, shortCircuitingCannedActionsHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

#pragma warning disable CS0618 // Type or member is obsolete
		Assert.IsFalse(requestMessage.Properties.ContainsKey(RequestProperties.PollyRetryAttempt));
#pragma warning restore CS0618 // Type or member is obsolete
	}

	[Test]
	[TestCase(1)]
	[TestCase(2)]
	public async Task When_N_Retries_PollyRetryAttempt_Property_Is_Present(int retryCount)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var shortCircuitingCannedActionsHandler = new ShortCircuitingCannedActionsHandler(
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)404 },
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)200, Content = new StringContent("yawn!") },
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)200 });
		var retrySignalingOnConditionHandler = new RetrySignalingOnConditionHandler();
		var retryHandler = new ExponentialBackoffWithJitterRetryOnSignalHandler(
			new RetrySettings
			{
				RetryCount = retryCount,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, retrySignalingOnConditionHandler, shortCircuitingCannedActionsHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

#pragma warning disable CS0618 // Type or member is obsolete
		Assert.AreEqual(retryCount, requestMessage.Properties[RequestProperties.PollyRetryAttempt]);
#pragma warning restore CS0618 // Type or member is obsolete
	}
}
