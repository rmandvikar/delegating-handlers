using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ShortCircuitingCannedActionsHandlerTests
{
	[Test]
	public async Task ShortCircuits()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var cannedResponses = new[] { fixture.Create<HttpResponseMessage>(), fixture.Create<HttpResponseMessage>() };
		var cannedActions = cannedResponses.Select(x => new Func<HttpRequestMessage, HttpResponseMessage>((request) => x)).ToArray();

		var shortCircuitingCannedActionsHandler = new ShortCircuitingCannedActionsHandler(cannedActions);

		using var invoker = HttpMessageInvokerFactory.Create(
			shortCircuitingCannedActionsHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var response1 = await invoker.SendAsync(requestMessage, CancellationToken.None);
		using var response2 = await invoker.SendAsync(requestMessage, CancellationToken.None);
		var responses = new[] { response1, response2 };

		Assert.AreEqual(cannedResponses, responses);
	}

	[Test]
	public async Task Continues_When_Exhausted()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var cannedResponses = new[] { fixture.Create<HttpResponseMessage>() };
		var cannedActions = cannedResponses.Select(x => new Func<HttpRequestMessage, HttpResponseMessage>((request) => x)).ToArray();

		var shortCircuitingCannedActionsHandler = new ShortCircuitingCannedActionsHandler(cannedActions);

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), shortCircuitingCannedActionsHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();

		using var response1 = await invoker.SendAsync(requestMessage, CancellationToken.None);
		using var response2 = await invoker.SendAsync(requestMessage, CancellationToken.None);
	}
}
