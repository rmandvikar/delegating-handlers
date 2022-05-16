using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class NullResponseHandlerTests
	{
		[Test]
		public async Task Nulls()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var nullResponseHandler = new NullResponseHandler();
			nullResponseHandler.InnerHandler = fixture.Create<HttpMessageHandler>();

			using var invoker = new HttpMessageInvoker(nullResponseHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

			Assert.IsNull(response);
		}
	}
}
