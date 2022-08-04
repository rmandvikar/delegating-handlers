using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class ShortCircuitingCannedResponsesHandlerTests
	{
		[Test]
		public async Task ShortCircuits()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var cannedResponses = new[] { fixture.Create<HttpResponseMessage>(), fixture.Create<HttpResponseMessage>() };
			var shortCircuitingCannedResponsesHandler = new ShortCircuitingCannedResponsesHandler(cannedResponses);
			shortCircuitingCannedResponsesHandler.InnerHandler = fixture.Create<HttpMessageHandler>();

			using var invoker = new HttpMessageInvoker(shortCircuitingCannedResponsesHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			using var response1 = await invoker.SendAsync(requestMessage, CancellationToken.None);
			using var response2 = await invoker.SendAsync(requestMessage, CancellationToken.None);
			var responses = new[] { response1, response2 };

			Assert.AreEqual(cannedResponses, responses);
		}
	}
}
