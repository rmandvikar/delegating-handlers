using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;
using rm.DelegatingHandlers;
using Serilog;
using Serilog.Core;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ServicePointLoggingHandlerTests
{
	[Test]
	public async Task Logs_ServicePoint_Stats()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var loggerMock = fixture.Freeze<Mock<ILogger>>();
		loggerMock.Setup(x => x.ForContext(It.IsAny<Type>())).Returns(loggerMock.Object);
		loggerMock.Setup(x => x.ForContext(It.IsAny<IEnumerable<ILogEventEnricher>>())).Returns(loggerMock.Object);
		var servicePointLoggingHandler = new ServicePointLoggingHandler(loggerMock.Object);

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), servicePointLoggingHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

		loggerMock.Verify((x) =>
			x.Information("ServicePoint stats"),
			Times.Once);
	}
}
