using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class NullResponseHandlerTests
{
	[Test]
	public async Task Nulls()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var nullResponseHandler = new NullResponseHandler();

		using var invoker = HttpMessageInvokerFactory.Create(
			nullResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.IsNull(response);
	}
}
