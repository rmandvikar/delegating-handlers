using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class SwallowingHandlerTests
	{
		[Test]
		public void Swallows()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var exception = new TurnDownForWhatException();
			var throwingHandler = new ThrowingHandler(exception);
			var swallowingHandler = new SwallowingHandler(
				ex => ex is TurnDownForWhatException);

			using var invoker = HttpMessageInvokerFactory.Create(
				fixture.Create<HttpMessageHandler>(), swallowingHandler, throwingHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			Assert.DoesNotThrowAsync(async () =>
			{
				using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
			});
		}

		[Test]
		public void Does_Not_Swallow()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var exception = new Exception();
			var throwingHandler = new ThrowingHandler(exception);
			var swallowingHandler = new SwallowingHandler(
				ex => ex is TurnDownForWhatException);

			using var invoker = HttpMessageInvokerFactory.Create(
				fixture.Create<HttpMessageHandler>(), swallowingHandler, throwingHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			Assert.ThrowsAsync<Exception>(async () =>
			{
				using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
			});
		}
	}
}
