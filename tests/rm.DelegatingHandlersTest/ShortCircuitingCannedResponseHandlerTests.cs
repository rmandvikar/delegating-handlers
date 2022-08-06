using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class ShortCircuitingCannedResponseHandlerTests
	{
		[Test]
		public async Task ShortCircuits()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var cannedResponse = fixture.Create<HttpResponseMessage>();
			var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(cannedResponse);

			using var invoker = HttpMessageInvokerFactory.Create(
				shortCircuitingCannedResponseHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

			Assert.AreEqual(response, cannedResponse);
		}
	}
}
