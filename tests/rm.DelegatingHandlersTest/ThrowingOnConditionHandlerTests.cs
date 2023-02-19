using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ThrowingOnConditionHandlerTests
{
	[Test]
	public void Throws()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var throwingOnConditionHandler = new ThrowingOnConditionHandler(new TurnDownForWhatException());

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), throwingOnConditionHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
#pragma warning disable CS0618 // Type or member is obsolete
		requestMessage.Properties[typeof(ThrowingOnConditionHandler).FullName!] = true;
#pragma warning restore CS0618 // Type or member is obsolete
		var ex = Assert.ThrowsAsync<TurnDownForWhatException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
	}

	[Test]
	[TestCase(true, false)]
	[TestCase(true, null)]
	[TestCase(false, null)]
	public void Does_Not_Throw(bool isValuePresent, object value)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var throwingOnConditionHandler = new ThrowingOnConditionHandler(new TurnDownForWhatException());

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), throwingOnConditionHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		if (isValuePresent)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			requestMessage.Properties[typeof(ThrowingOnConditionHandler).FullName!] = value;
#pragma warning restore CS0618 // Type or member is obsolete
		}
		Assert.DoesNotThrowAsync(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
	}

	[Test]
	[TestCase(0)]
	public void Throws_On_Invalid_Value(object value)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var throwingOnConditionHandler = new ThrowingOnConditionHandler(new TurnDownForWhatException());

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), throwingOnConditionHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
#pragma warning disable CS0618 // Type or member is obsolete
		requestMessage.Properties[typeof(ThrowingOnConditionHandler).FullName!] = value;
#pragma warning restore CS0618 // Type or member is obsolete
		Assert.ThrowsAsync<InvalidCastException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});
	}
}
