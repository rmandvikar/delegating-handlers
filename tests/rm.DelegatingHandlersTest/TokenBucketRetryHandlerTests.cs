using System.Net;
using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;
using rm.Clock;
using rm.DelegatingHandlers;
using rm.Random2;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class TokenBucketRetryHandlerTests
{
	private static readonly Random rng = RandomFactory.GetThreadStaticRandom();

	[Test]
	public void Throws_TokenBucketRetryException()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var content = fixture.Create<string>();
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = (HttpStatusCode)500,
				Content = content,
			});
		var tokenBucketRetryHandler = new TokenBucketRetryHandler(
			new TokenBucketRetryHandlerSettings
			{
				Percentage = 0.10d,
			});
		var clockMock = fixture.Freeze<Mock<ISystemClock>>();
		clockMock.Setup(x => x.UtcNow).Returns(DateTimeOffsetValues.Chernobyl);
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 2,
				RetryDelayInMilliseconds = 0,
			},
			clockMock.Object);

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, tokenBucketRetryHandler, shortCircuitingResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		var ex = Assert.ThrowsAsync<TokenBucketRetryException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
		Console.WriteLine(ex!.Message);
	}

	[Test]
	public async Task Does_Not_Throw_TokenBucketRetryException()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var content = fixture.Create<string>();
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = (HttpStatusCode)200,
				Content = content,
			});
		var tokenBucketRetryHandler = new TokenBucketRetryHandler(
			new TokenBucketRetryHandlerSettings
			{
				Percentage = 0.10d,
			});
		var clockMock = fixture.Freeze<Mock<ISystemClock>>();
		clockMock.Setup(x => x.UtcNow).Returns(DateTimeOffsetValues.Chernobyl);
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 2,
				RetryDelayInMilliseconds = 0,
			},
			clockMock.Object);

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, tokenBucketRetryHandler, shortCircuitingResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
	}

	[Explicit]
	[Test]
	public async Task Does_Not_Throw_TokenBucketRetryException_Iterations()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var content = fixture.Create<string>();
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = (HttpStatusCode)200,
				Content = content,
			});
		var tokenBucketRetryHandler = new TokenBucketRetryHandler(
			new TokenBucketRetryHandlerSettings
			{
				Percentage = 0.05d,
			});
		var clockMock = fixture.Freeze<Mock<ISystemClock>>();
		clockMock.Setup(x => x.UtcNow).Returns(DateTimeOffsetValues.Chernobyl);
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 2,
				RetryDelayInMilliseconds = 0,
			},
			clockMock.Object);

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, tokenBucketRetryHandler, shortCircuitingResponseHandler);

		const int iterations = 1_000;
		for (int i = 0; i < iterations; i++)
		{
			using var requestMessage = fixture.Create<HttpRequestMessage>();
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		}
	}

	[Explicit]
	[Test]
	public async Task Does_Not_Throw_TokenBucketRetryException_Probability_Iterations()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = (HttpStatusCode)200,
				Content = fixture.Create<string>(),
			});
		var shortCircuitingResponseWithProbabilityHandler = new ShortCircuitingResponseWithProbabilityHandler(
			new ShortCircuitingResponseWithProbabilityHandlerSettings
			{
				ProbabilityPercentage = 0.1d,
				StatusCode = (HttpStatusCode)500,
				Content = fixture.Create<string>(),
			},
			rng);
		var tokenBucketRetryHandler = new TokenBucketRetryHandler(
			new TokenBucketRetryHandlerSettings
			{
				Percentage = 0.10d,
			});
		var clockMock = fixture.Freeze<Mock<ISystemClock>>();
		clockMock.Setup(x => x.UtcNow).Returns(DateTimeOffsetValues.Chernobyl);
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 2,
				RetryDelayInMilliseconds = 0,
			},
			clockMock.Object);

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, tokenBucketRetryHandler, shortCircuitingResponseWithProbabilityHandler, shortCircuitingResponseHandler);

		const int iterations = 1_000;
		const int batchSize = 100;
		for (int i = 0; i < iterations; i += batchSize)
		{
			var tasks = new List<Task>(batchSize);
			for (int b = 0; b < batchSize; b++)
			{
				using var requestMessage = fixture.Create<HttpRequestMessage>();
				tasks.Add(invoker.SendAsync(requestMessage, CancellationToken.None));
			}
			await Task.WhenAll(tasks);
		}
	}
}
