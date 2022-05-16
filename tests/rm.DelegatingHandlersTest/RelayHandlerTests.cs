using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class RelayHandlerTests
	{
		[Test]
		public async Task Relays()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var relayHandler = new RelayHandler();
			relayHandler.InnerHandler = fixture.Create<HttpMessageHandler>();

			using var invoker = new HttpMessageInvoker(relayHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		}
	}
}
