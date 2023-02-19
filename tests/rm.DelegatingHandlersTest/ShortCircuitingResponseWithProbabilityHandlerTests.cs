using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ShortCircuitingResponseWithProbabilityHandlerTests
{
	[Test]
	public async Task ShortCircuits()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var statusCode = (HttpStatusCode)42;
		var content = fixture.Create<string>();
		var shortCircuitingResponseWithProbabilityHandler = new ShortCircuitingResponseWithProbabilityHandler(
			new ShortCircuitingResponseWithProbabilityHandlerSettings
			{
				ProbabilityPercentage = 100d,
				StatusCode = statusCode,
				Content = content,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			shortCircuitingResponseWithProbabilityHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(statusCode, response.StatusCode);
		Assert.AreEqual(content, await response.Content.ReadAsStringAsync());
		Assert.AreEqual($"{nameof(ShortCircuitingResponseWithProbabilityHandler)} says hello!", response.ReasonPhrase);
	}

	[Test]
	public async Task Does_Not_ShortCircuit()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var statusCode = (HttpStatusCode)42;
		var content = fixture.Create<string>();
		var shortCircuitingResponseWithProbabilityHandler = new ShortCircuitingResponseWithProbabilityHandler(
			new ShortCircuitingResponseWithProbabilityHandlerSettings
			{
				ProbabilityPercentage = 0d,
				StatusCode = statusCode,
				Content = content,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), shortCircuitingResponseWithProbabilityHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreNotEqual(statusCode, response.StatusCode);
	}
}
