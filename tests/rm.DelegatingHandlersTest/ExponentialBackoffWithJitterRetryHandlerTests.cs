using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class ExponentialBackoffWithJitterRetryHandlerTests
	{
		[Test]
		public async Task Retries_On_5xx()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var statusCode = (HttpStatusCode)542;
			var content = fixture.Create<string>();
			var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
				new ShortCircuitingResponseHandlerSettings
				{
					StatusCode = statusCode,
					Content = content,
				});
			var retryAttempt = -1;
			var delegateHandler = new DelegateHandler(
				(request, ct) =>
				{
					retryAttempt++;
					return Task.CompletedTask;
				});
			var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
				new RetrySettings
				{
					RetryCount = 1,
					RetryDelayInMilliseconds = 0,
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				fixture.Create<HttpMessageHandler>(), retryHandler, delegateHandler, shortCircuitingResponseHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

			Assert.AreEqual(1, retryAttempt);
		}

		[Test]
		public void Retries_On_HttpRequestException()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var throwingHandler = new ThrowingHandler(new HttpRequestException());
			var retryAttempt = -1;
			var delegateHandler = new DelegateHandler(
				(request, ct) =>
				{
					retryAttempt++;
					return Task.CompletedTask;
				});
			var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
				new RetrySettings
				{
					RetryCount = 1,
					RetryDelayInMilliseconds = 0,
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				retryHandler, delegateHandler, throwingHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			Assert.ThrowsAsync<HttpRequestException>(async () =>
			{
				using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
			});

			Assert.AreEqual(1, retryAttempt);
		}

		[Test]
		public void Does_Not_Retries_On_TaskCanceledException()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var throwingHandler = new ThrowingHandler(new TaskCanceledException());
			var retryAttempt = -1;
			var delegateHandler = new DelegateHandler(
				(request, ct) =>
				{
					retryAttempt++;
					return Task.CompletedTask;
				});
			var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
				new RetrySettings
				{
					RetryCount = 1,
					RetryDelayInMilliseconds = 0,
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				fixture.Create<HttpMessageHandler>(), retryHandler, delegateHandler, throwingHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			Assert.ThrowsAsync<TaskCanceledException>(async () =>
			{
				using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
			});

			Assert.AreEqual(0, retryAttempt);
		}

		[Test]
		public async Task When_0_Retries_PollyRetryAttempt_Property_Is_Not_Present()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var statusCode = (HttpStatusCode)542;
			var content = fixture.Create<string>();
			var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
				new ShortCircuitingResponseHandlerSettings
				{
					StatusCode = statusCode,
					Content = content,
				});
			var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
				new RetrySettings
				{
					RetryCount = 0,
					RetryDelayInMilliseconds = 0,
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				fixture.Create<HttpMessageHandler>(), retryHandler, shortCircuitingResponseHandler);

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

			var statusCode = (HttpStatusCode)542;
			var content = fixture.Create<string>();
			var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
				new ShortCircuitingResponseHandlerSettings
				{
					StatusCode = statusCode,
					Content = content,
				});
			var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
				new RetrySettings
				{
					RetryCount = retryCount,
					RetryDelayInMilliseconds = 0,
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				retryHandler, shortCircuitingResponseHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

#pragma warning disable CS0618 // Type or member is obsolete
			Assert.AreEqual(retryCount, requestMessage.Properties[RequestProperties.PollyRetryAttempt]);
#pragma warning restore CS0618 // Type or member is obsolete
		}
	}
}
