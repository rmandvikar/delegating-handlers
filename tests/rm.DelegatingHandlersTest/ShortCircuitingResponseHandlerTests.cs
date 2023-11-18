using System.Net;
using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ShortCircuitingResponseHandlerTests
{
	[Test]
	public async Task ShortCircuits()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var statusCode = (HttpStatusCode)42;
		var content = fixture.Create<string>();
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = statusCode,
				Content = content,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			shortCircuitingResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(statusCode, response.StatusCode);
		Assert.AreEqual(content, await response.Content.ReadAsStringAsync());
		Assert.AreEqual($"{nameof(ShortCircuitingResponseHandler)} says hello!", response.ReasonPhrase);
	}
}
