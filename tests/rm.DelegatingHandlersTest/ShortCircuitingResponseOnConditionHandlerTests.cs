using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class ShortCircuitingResponseOnConditionHandlerTests
	{
		[Test]
		public async Task ShortCircuits()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var statusCode = (HttpStatusCode)42;
			var content = fixture.Create<string>();
			var shortCircuitingResponseOnConditionHandler = new ShortCircuitingResponseOnConditionHandler(
				new ShortCircuitingResponseOnConditionHandlerSettings
				{
					StatusCode = statusCode,
					Content = content,
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				shortCircuitingResponseOnConditionHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
#pragma warning disable CS0618 // Type or member is obsolete
			requestMessage.Properties[typeof(ShortCircuitingResponseOnConditionHandler).FullName!] = true;
#pragma warning restore CS0618 // Type or member is obsolete
			using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

			Assert.AreEqual(statusCode, response.StatusCode);
			Assert.AreEqual(content, await response.Content.ReadAsStringAsync());
			Assert.AreEqual($"{nameof(ShortCircuitingResponseOnConditionHandler)} says hello!", response.ReasonPhrase);
		}

		[Test]
		[TestCase(true, false)]
		[TestCase(true, null)]
		[TestCase(false, null)]
		public async Task Does_Not_ShortCircuit(bool isValuePresent, object value)
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var statusCode = (HttpStatusCode)42;
			var content = fixture.Create<string>();
			var shortCircuitingResponseOnConditionHandler = new ShortCircuitingResponseOnConditionHandler(
				new ShortCircuitingResponseOnConditionHandlerSettings
				{
					StatusCode = statusCode,
					Content = content,
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				fixture.Create<HttpMessageHandler>(), shortCircuitingResponseOnConditionHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			if (isValuePresent)
			{
#pragma warning disable CS0618 // Type or member is obsolete
				requestMessage.Properties[typeof(ShortCircuitingResponseOnConditionHandler).FullName!] = value;
#pragma warning restore CS0618 // Type or member is obsolete
			}

			using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

			Assert.AreNotEqual(statusCode, response.StatusCode);
		}

		[Test]
		[TestCase(0)]
		public void Throws_On_Invalid_Value(object value)
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var statusCode = (HttpStatusCode)42;
			var content = fixture.Create<string>();
			var shortCircuitingResponseOnConditionHandler = new ShortCircuitingResponseOnConditionHandler(
				new ShortCircuitingResponseOnConditionHandlerSettings
				{
					StatusCode = statusCode,
					Content = content,
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				shortCircuitingResponseOnConditionHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
#pragma warning disable CS0618 // Type or member is obsolete
			requestMessage.Properties[typeof(ShortCircuitingResponseOnConditionHandler).FullName!] = value;
#pragma warning restore CS0618 // Type or member is obsolete
			Assert.ThrowsAsync<InvalidCastException>(async () =>
			{
				using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
			});
		}
	}
}
