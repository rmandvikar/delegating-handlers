using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class RequestHeaderHandlerTests
{
	[Test]
	[TestCase("headerName", "headerValue")]
	[TestCase("authorization", "Bearer token")]
	public async Task Adds_Header(string headerName, string headerValue)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var requestHeaderHandler = new RequestHeaderHandler(headerName, headerValue, HttpHeaderTarget.Message);
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = (HttpStatusCode)42,
				Content = fixture.Create<string>(),
			});
		using var invoker = HttpMessageInvokerFactory.Create(
			requestHeaderHandler, shortCircuitingResponseHandler);
		using var request = fixture.Create<HttpRequestMessage>();
		using var response = await invoker.SendAsync(request, CancellationToken.None);

		Assert.IsTrue(request.Headers.TryGetValue(headerName, out var value));
		Assert.AreEqual(headerValue, value);
	}

	[Test]
	[TestCase("content.headerName", "content.headerValue")]
	[TestCase("content.authorization", "Bearer token")]
	public async Task Adds_Header_Content(string headerName, string headerValue)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var requestHeaderHandler = new RequestHeaderHandler(headerName, headerValue, HttpHeaderTarget.MessageContent);
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = (HttpStatusCode)42,
				Content = fixture.Create<string>(),
			});
		using var invoker = HttpMessageInvokerFactory.Create(
			requestHeaderHandler, shortCircuitingResponseHandler);
		using var request = fixture.Create<HttpRequestMessage>();
		using var response = await invoker.SendAsync(request, CancellationToken.None);

		Assert.IsTrue(request.Content!.Headers.TryGetValue(headerName, out var value));
		Assert.AreEqual(headerValue, value);
	}
}
