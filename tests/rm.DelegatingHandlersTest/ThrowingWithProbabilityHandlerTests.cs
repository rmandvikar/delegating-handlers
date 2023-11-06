using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;
using rm.Random2;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ThrowingWithProbabilityHandlerTests
{
	private static readonly Random rng = RandomFactory.GetThreadStaticRandom();

	[Test]
	public void Throws()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var throwingWithProbabilityHandler = new ThrowingWithProbabilityHandler(100d, new TurnDownForWhatException(), rng);

		using var invoker = HttpMessageInvokerFactory.Create(
			throwingWithProbabilityHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		var ex = Assert.ThrowsAsync<TurnDownForWhatException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
	}

	[Test]
	public void Does_Not_Throw()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var throwingWithProbabilityHandler = new ThrowingWithProbabilityHandler(0d, new TurnDownForWhatException(), rng);

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), throwingWithProbabilityHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		Assert.DoesNotThrowAsync(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
	}
}
