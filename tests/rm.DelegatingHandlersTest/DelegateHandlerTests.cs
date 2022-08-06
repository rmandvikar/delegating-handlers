using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class DelegateHandlerTests
	{
		[Test]
		public async Task Delegates_Pre()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var i = 0;
			var delegateHandler = new DelegateHandler(
				preDelegate: (request, ct) =>
				{
					i++;
					return Task.CompletedTask;
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				fixture.Create<HttpMessageHandler>(), delegateHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

			Assert.AreEqual(1, i);
		}

		[Test]
		public async Task Delegates_Post()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var i = 0;
			var delegateHandler = new DelegateHandler(
				postDelegate: (request, response, ct) =>
				{
					i++;
					return Task.CompletedTask;
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				fixture.Create<HttpMessageHandler>(), delegateHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

			Assert.AreEqual(1, i);
		}
	}
}
