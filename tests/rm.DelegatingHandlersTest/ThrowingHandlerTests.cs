using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class ThrowingHandlerTests
	{
		[Test]
		public void Throws()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var throwingHandler = new ThrowingHandler(new TurnDownForWhatException());
			throwingHandler.InnerHandler = fixture.Create<HttpMessageHandler>();

			using var invoker = new HttpMessageInvoker(throwingHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			var ex = Assert.ThrowsAsync<TurnDownForWhatException>(async () =>
			{
				using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
			});
		}
	}
}
