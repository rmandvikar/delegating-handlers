﻿using System.Net;
using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class RetryAfterDelayHandlerTests
{
	[Test]
	[TestCase(503)]
	[TestCase(429)]
	[TestCase(301)]
	public async Task Adds_RetryAfter_Header_With_Seconds(int statusCode)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var cannedResponse = new HttpResponseMessage
		{
			StatusCode = (HttpStatusCode)statusCode,
		};
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(cannedResponse);
		var delayInSeconds = 42;
		var retryAfterDelayHandler = new RetryAfterDelayHandler(delayInSeconds);

		using var invoker = HttpMessageInvokerFactory.Create(
			retryAfterDelayHandler, shortCircuitingCannedResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(delayInSeconds.ToString(), response.Headers.GetValues(ResponseHeaders.RetryAfter).Single());
	}

	[Test]
	[TestCase(200)]
	public async Task Does_Not_Add_RetryAfter_Header(int statusCode)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var cannedResponse = new HttpResponseMessage
		{
			StatusCode = (HttpStatusCode)statusCode,
		};
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(cannedResponse);
		var delayInSeconds = 42;
		var retryAfterDelayHandler = new RetryAfterDelayHandler(delayInSeconds);

		using var invoker = HttpMessageInvokerFactory.Create(
			retryAfterDelayHandler, shortCircuitingCannedResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.IsFalse(response.Headers.Contains(ResponseHeaders.RetryAfter));
	}
}
