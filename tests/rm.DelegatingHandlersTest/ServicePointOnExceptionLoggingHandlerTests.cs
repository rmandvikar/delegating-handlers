using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;
using rm.DelegatingHandlers;
using Serilog;
using Serilog.Core;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ServicePointOnExceptionLoggingHandlerTests
{
	[Test]
	public async Task Does_Not_Log_ServicePoint_Stats()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var loggerMock = fixture.Freeze<Mock<ILogger>>();
		loggerMock.Setup(x => x.ForContext(It.IsAny<Type>())).Returns(loggerMock.Object);
		loggerMock.Setup(x => x.ForContext(It.IsAny<IEnumerable<ILogEventEnricher>>())).Returns(loggerMock.Object);
		var servicePointOnExceptionLoggingHandler = new ServicePointOnExceptionLoggingHandler(loggerMock.Object);

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), servicePointOnExceptionLoggingHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

		loggerMock.Verify((x) =>
			x.Error(It.IsAny<string>()),
			Times.Never);
	}

	[Test]
	public void Logs_ServicePoint_Stats_During_Exception()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var loggerMock = fixture.Freeze<Mock<ILogger>>();
		loggerMock.Setup(x => x.ForContext(It.IsAny<Type>())).Returns(loggerMock.Object);
		loggerMock.Setup(x => x.ForContext(It.IsAny<IEnumerable<ILogEventEnricher>>())).Returns(loggerMock.Object);
		var servicePointOnExceptionLoggingHandler = new ServicePointOnExceptionLoggingHandler(loggerMock.Object);
		var throwingHandler = new ThrowingHandler(new TurnDownForWhatException());

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), servicePointOnExceptionLoggingHandler, throwingHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		var ex = Assert.ThrowsAsync<TurnDownForWhatException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});

		loggerMock.Verify((x) =>
			x.Error(ex, "ServicePoint stats"),
			Times.Once);
	}
}
