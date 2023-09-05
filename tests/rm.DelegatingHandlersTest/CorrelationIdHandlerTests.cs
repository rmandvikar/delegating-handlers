using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class CorrelationIdHandlerTests
{
	[Test]
	public async Task Adds_CorrelationId()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(new HttpResponseMessage(HttpStatusCode.OK));
		var correlationId = "boom!";
		var correlationIdContextMock = fixture.Freeze<Mock<ICorrelationIdContext>>();
		correlationIdContextMock.Setup(x => x.GetValue()).Returns(correlationId);
		var correlationIdHandler = fixture.Create<CorrelationIdHandler>();
		using var invoker = HttpMessageInvokerFactory.Create(
			correlationIdHandler, shortCircuitingCannedResponseHandler);
		using var request = new HttpRequestMessage();

		using var response = await invoker.SendAsync(request, CancellationToken.None);

		Assert.IsTrue(request.Headers.Contains(RequestHeaders.CorrelationId));
		Assert.AreEqual(correlationId, request.Headers.GetValues(RequestHeaders.CorrelationId).Single());
		Assert.IsTrue(response.Headers.Contains(ResponseHeaders.CorrelationId));
		Assert.AreEqual(correlationId, response.Headers.GetValues(RequestHeaders.CorrelationId).Single());
	}

	[Test]
	public async Task Adds_Multiple_CorrelationIds()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(new HttpResponseMessage(HttpStatusCode.OK));
		var correlationId1 = "boom1!";
		var correlationId2 = "boom2!";
		var correlationIdContextMock = fixture.Freeze<Mock<ICorrelationIdContext>>();
		correlationIdContextMock.Setup(x => x.GetValue()).Returns(new StringValues(new[] { correlationId1, correlationId2, }));
		var correlationIdHandler = fixture.Create<CorrelationIdHandler>();
		using var invoker = HttpMessageInvokerFactory.Create(
			correlationIdHandler, shortCircuitingCannedResponseHandler);
		using var request = new HttpRequestMessage();

		using var response = await invoker.SendAsync(request, CancellationToken.None);

		Assert.IsTrue(request.Headers.Contains(RequestHeaders.CorrelationId));
		Assert.AreEqual(new[] { correlationId1, correlationId2 }, request.Headers.GetValues(RequestHeaders.CorrelationId));

		Assert.IsTrue(response.Headers.Contains(RequestHeaders.CorrelationId));
		Assert.AreEqual(new[] { correlationId1, correlationId2 }, response.Headers.GetValues(RequestHeaders.CorrelationId));
	}

	[Test]
	public void Throws_If_CorrelationId_Values_Is_Null_Value()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(new HttpResponseMessage(HttpStatusCode.OK));
		var correlationIdContextMock = fixture.Freeze<Mock<ICorrelationIdContext>>();
		correlationIdContextMock.Setup(x => x.GetValue()).Returns(new StringValues(new string[] { null!, }));
		var correlationIdHandler = fixture.Create<CorrelationIdHandler>();
		using var invoker = HttpMessageInvokerFactory.Create(
			correlationIdHandler, shortCircuitingCannedResponseHandler);
		using var request = new HttpRequestMessage();

		Assert.ThrowsAsync<InvalidOperationException>(async () =>
		{
			using var response = await invoker.SendAsync(request, CancellationToken.None);
		});
	}

	[Test]
	public void Throws_If_CorrelationId_Values_Is_Empty_Value()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(new HttpResponseMessage(HttpStatusCode.OK));
		var correlationIdContextMock = fixture.Freeze<Mock<ICorrelationIdContext>>();
		correlationIdContextMock.Setup(x => x.GetValue()).Returns(new StringValues(new string[] { }));
		var correlationIdHandler = fixture.Create<CorrelationIdHandler>();
		using var invoker = HttpMessageInvokerFactory.Create(
			correlationIdHandler, shortCircuitingCannedResponseHandler);
		using var request = new HttpRequestMessage();

		Assert.ThrowsAsync<InvalidOperationException>(async () =>
		{
			using var response = await invoker.SendAsync(request, CancellationToken.None);
		});
	}

	[Test]
	public void Throws_If_CorrelationId_Values_Is_WhiteSpace_Value()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(new HttpResponseMessage(HttpStatusCode.OK));
		var correlationIdContextMock = fixture.Freeze<Mock<ICorrelationIdContext>>();
		correlationIdContextMock.Setup(x => x.GetValue()).Returns(new StringValues(new string[] { "" }));
		var correlationIdHandler = fixture.Create<CorrelationIdHandler>();
		using var invoker = HttpMessageInvokerFactory.Create(
			correlationIdHandler, shortCircuitingCannedResponseHandler);
		using var request = new HttpRequestMessage();

		Assert.ThrowsAsync<InvalidOperationException>(async () =>
		{
			using var response = await invoker.SendAsync(request, CancellationToken.None);
		});
	}

	[Test]
	public async Task Does_Not_Add_CorrelationId_To_Response_If_Same_Value_Already_Present()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(new HttpResponseMessage(HttpStatusCode.OK));
		var correlationIdAlreadyPresent = "boom!";
		var addsCorrelationIdToResponseHandler = new AddsCorrelationIdToResponseHandler(correlationIdAlreadyPresent);
		var correlationId = "boom!";
		var correlationIdContextMock = fixture.Freeze<Mock<ICorrelationIdContext>>();
		correlationIdContextMock.Setup(x => x.GetValue()).Returns(correlationId);
		var correlationIdHandler = fixture.Create<CorrelationIdHandler>();
		using var invoker = HttpMessageInvokerFactory.Create(
			correlationIdHandler, addsCorrelationIdToResponseHandler, shortCircuitingCannedResponseHandler);
		using var request = new HttpRequestMessage();

		using var response = await invoker.SendAsync(request, CancellationToken.None);

		Assert.IsTrue(response.Headers.Contains(RequestHeaders.CorrelationId));
		Assert.AreEqual(correlationIdAlreadyPresent, response.Headers.GetValues(RequestHeaders.CorrelationId).Single());
	}

	[Test]
	public async Task Adds_CorrelationId_To_Response_If_Different_Value_Present()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(new HttpResponseMessage(HttpStatusCode.OK));
		var correlationIdAlreadyPresent = "oops!";
		var addsCorrelationIdToResponseHandler = new AddsCorrelationIdToResponseHandler(correlationIdAlreadyPresent);
		var correlationId = "boom!";
		var correlationIdContextMock = fixture.Freeze<Mock<ICorrelationIdContext>>();
		correlationIdContextMock.Setup(x => x.GetValue()).Returns(correlationId);
		var correlationIdHandler = fixture.Create<CorrelationIdHandler>();
		using var invoker = HttpMessageInvokerFactory.Create(
			correlationIdHandler, addsCorrelationIdToResponseHandler, shortCircuitingCannedResponseHandler);
		using var request = new HttpRequestMessage();

		using var response = await invoker.SendAsync(request, CancellationToken.None);

		Assert.IsTrue(response.Headers.Contains(RequestHeaders.CorrelationId));
		Assert.AreEqual(new[] { correlationIdAlreadyPresent, correlationId }, response.Headers.GetValues(RequestHeaders.CorrelationId));
	}

	[Test]
	public async Task Adds_CorrelationId_Deduped_To_Response_If_Different_Value_Present()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(new HttpResponseMessage(HttpStatusCode.OK));
		var correlationIdAlreadyPresent = "boom2!";
		var addsCorrelationIdToResponseHandler = new AddsCorrelationIdToResponseHandler(correlationIdAlreadyPresent);
		var correlationId1 = "boom1!";
		var correlationId2 = "boom2!";
		var correlationIdContextMock = fixture.Freeze<Mock<ICorrelationIdContext>>();
		correlationIdContextMock.Setup(x => x.GetValue()).Returns(new StringValues(new[] { correlationId1, correlationId2, }));
		var correlationIdHandler = fixture.Create<CorrelationIdHandler>();

		using var invoker = HttpMessageInvokerFactory.Create(
			correlationIdHandler, addsCorrelationIdToResponseHandler, shortCircuitingCannedResponseHandler);
		using var request = new HttpRequestMessage();

		using var response = await invoker.SendAsync(request, CancellationToken.None);

		Assert.IsTrue(response.Headers.Contains(RequestHeaders.CorrelationId));
		Assert.AreEqual(new[] { correlationId2, correlationId1 }, response.Headers.GetValues(RequestHeaders.CorrelationId));
	}
}
