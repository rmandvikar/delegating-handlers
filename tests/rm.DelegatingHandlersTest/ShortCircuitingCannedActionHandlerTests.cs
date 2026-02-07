using System.Net;
using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ShortCircuitingCannedActionHandlerTests
{
	[Test]
	public async Task ShortCircuits()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		HttpResponseMessage cannedResponse = null!;
		var cannedAction = new Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>
			(
				async (request, cancellationToken) =>
				{
					// gen a new http response on every call
					cannedResponse = new HttpResponseMessage() { StatusCode = (HttpStatusCode)200, Content = new StringContent("_") };
					return cannedResponse;
				}
			);

		var shortCircuitingCannedActionHandler = new ShortCircuitingCannedActionHandler(cannedAction);

		using var invoker = HttpMessageInvokerFactory.Create(
			shortCircuitingCannedActionHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(cannedResponse, response);
	}

	[Test]
	public async Task ShortCircuits_Multiple()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		for (int i = 0; i < 2; i++)
		{
			HttpResponseMessage cannedResponse = null!;
			var cannedAction = new Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>
				(
					async (request, cancellationToken) =>
					{
						// gen a new http response on every call
						cannedResponse = new HttpResponseMessage() { StatusCode = (HttpStatusCode)200, Content = new StringContent("_") };
						return cannedResponse;
					}
				);

			var shortCircuitingCannedActionHandler = new ShortCircuitingCannedActionHandler(cannedAction);

			using var invoker = HttpMessageInvokerFactory.Create(
				shortCircuitingCannedActionHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

			Assert.AreEqual(cannedResponse, response);
		}
	}
}
