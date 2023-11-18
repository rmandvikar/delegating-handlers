using System.Net.Http;
using NUnit.Framework;
using rm.DelegatingHandlers;
using rm.DelegatingHandlers.Formatting;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.InMemory;
using Serilog.Sinks.InMemory.Assertions;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class Http2HandlerTests
{
	[Test]
	public async Task Sends_Http2()
	{
		var logger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.WriteTo.InMemory()
			.WriteTo.Console(new JsonFormatter())
			.CreateLogger();
		var version = "2.0";
		var method = HttpMethod.Get;
		var uri = "https://httpbin.org";
		using var request =
			new HttpRequestMessage(method, uri);

		var http2Handler = new Http2Handler();
		var loggingHandler = new LoggingHandler(logger, new LoggingFormatter());

		using var httpclient = HttpClientFactory.Create(
#if NETFRAMEWORK
			new WinHttpHandler(),
#endif
			http2Handler, loggingHandler);

		using var _ = await httpclient.SendAsync(request, CancellationToken.None);

		InMemorySink.Instance.Should()
			.HaveMessage("request/").Appearing().Once()
				.WithProperty($"request.Version").WithValue(version)
			;
		InMemorySink.Instance.Should()
			.HaveMessage("request/response").Appearing().Once()
				.WithProperty($"response.Version").WithValue(version)
			;
	}
}
