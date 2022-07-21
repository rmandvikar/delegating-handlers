using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
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

			using var response = await invoker.SendAsync(request, new CancellationToken());

			Assert.IsTrue(request.Headers.Contains(RequestHeaders.CorrelationId));
			Assert.AreEqual(correlationId, request.Headers.GetValues(RequestHeaders.CorrelationId).Single());
			Assert.IsTrue(response.Headers.Contains(RequestHeaders.CorrelationId));
			Assert.AreEqual(correlationId, response.Headers.GetValues(RequestHeaders.CorrelationId).Single());
		}

		[Test]
		public async Task Does_Not_Add_CorrelationId_To_Response_If_Already_Present()
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

			using var response = await invoker.SendAsync(request, new CancellationToken());

			Assert.IsTrue(response.Headers.Contains(RequestHeaders.CorrelationId));
			Assert.AreEqual(correlationIdAlreadyPresent, response.Headers.GetValues(RequestHeaders.CorrelationId).Single());
		}
	}
}
