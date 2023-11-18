using System.Net;
using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class MockResponseHttpMessageHandlerTests
{
	[Test]
	public async Task Verify()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		using var http200 = new HttpResponseMessage(HttpStatusCode.OK);
		var mockResponseFactoryMock = fixture.Freeze<Mock<IMockResponseFactory>>();
		mockResponseFactoryMock
			.Setup(x => x.GetMockResponse(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(http200);
		var mockResponseHttpMessageHandler = fixture.Create<MockResponseHttpMessageHandler>();
		using var invoker = HttpMessageInvokerFactory.Create(
			mockResponseHttpMessageHandler);

		using var request = new HttpRequestMessage();
		using var response = await invoker.SendAsync(request, CancellationToken.None);

		Assert.IsNotNull(response);
		Assert.AreEqual(http200, response);
	}
}
