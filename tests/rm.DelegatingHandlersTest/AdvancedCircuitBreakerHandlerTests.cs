using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using Polly.CircuitBreaker;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class AdvancedCircuitBreakerHandlerTests
	{
		private const int iterations = 10;
		private static int[] handledStatusCodes =
			{
				500, // 5xx
				429,
			};
		private static Exception[] handledExceptions =
			{
				new TaskCanceledException(),
				new HttpRequestException(),
				new TimeoutExpiredException(),
			};
		private readonly Func<Exception, bool> handledExceptionsPredicate = (ex) =>
			ex is TaskCanceledException || ex is HttpRequestException || ex is TimeoutExpiredException;

		[Test]
		[TestCaseSource(nameof(handledStatusCodes))]
		public void CircuitBreaks_On_StatusCodes(int statusCode)
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var content = fixture.Create<string>();
			var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
				new ShortCircuitingResponseHandlerSettings
				{
					StatusCode = (HttpStatusCode)statusCode,
					Content = content,
				});
			var circuitBreaker = new AdvancedCircuitBreakerHandler(
				new AdvancedCircuitBreakerHandlerSettings
				{
					FailureThreshold = 0.0000001d,
					SamplingDuration = TimeSpan.FromSeconds(10),
					MinimumThroughput = 2,
					DurationOfBreak = TimeSpan.MaxValue,
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				circuitBreaker, shortCircuitingResponseHandler);

			Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
			{
				for (int i = 0; i < iterations; i++)
				{
					using var requestMessage = fixture.Create<HttpRequestMessage>();
					using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
				}
			});
		}

		[Test]
		[TestCaseSource(nameof(handledExceptions))]
		public void CircuitBreaks_On_Exceptions(Exception handledException)
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var throwingHandler = new ThrowingHandler(handledException);
			var swallowingHandler = new SwallowingHandler(handledExceptionsPredicate);
			var circuitBreaker = new AdvancedCircuitBreakerHandler(
				new AdvancedCircuitBreakerHandlerSettings
				{
					FailureThreshold = 0.0000001d,
					SamplingDuration = TimeSpan.FromSeconds(10),
					MinimumThroughput = 2,
					DurationOfBreak = TimeSpan.MaxValue,
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				swallowingHandler, circuitBreaker, throwingHandler);

			Assert.ThrowsAsync<BrokenCircuitException>(async () =>
			{
				for (int i = 0; i < iterations; i++)
				{
					using var requestMessage = fixture.Create<HttpRequestMessage>();
					using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
				}
			});
		}
	}
}
