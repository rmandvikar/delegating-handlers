using System.Net.Http;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using rm.DelegatingHandlers;
using rm.DelegatingHandlers.Formatting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.InMemory;
using Serilog.Sinks.InMemory.Assertions;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class LoggingPreHandlerTests
{
	public class Formatter
	{
		[Test]
		public async Task Logs_Request()
		{
			var logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.WriteTo.InMemory()
				.WriteTo.Console(new JsonFormatter())
				.CreateLogger();
			var version = "2.0";
			var method = HttpMethod.Post;
			var uri = "/health";
			var requestContent = "woot?!";
			var encoding = Encoding.UTF8;
			var mimeType = MediaTypeNames.Application.Json;
			var header1 = "header1";
			var headerValue1 = "headerValue1";
			var header2 = "header02";
			var headerValue2 = "headerValue02";
			var property1 = "property1";
			var propertyValue1 = "propertyValue1";
			var property2 = "property02";
			var propertyValue2 = "propertyValue02";
			using var request =
				new HttpRequestMessage(method, uri)
				{
					Version = new Version(version),
					Content = new StringContent(requestContent, encoding, mimeType),
				};
			request.Headers.Add(header1, headerValue1);
			request.Headers.Add(header2, headerValue2);
#pragma warning disable CS0618 // Type or member is obsolete
			request.Properties.Add(property1, propertyValue1);
			request.Properties.Add(property2, propertyValue2);
#pragma warning restore CS0618 // Type or member is obsolete

			var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(new HttpResponseMessage());
			var loggingPreHandler = new LoggingPreHandler(logger, new LoggingFormatter());

			using var invoker = HttpMessageInvokerFactory.Create(
				loggingPreHandler, shortCircuitingCannedResponseHandler);

			using var _ = await invoker.SendAsync(request, CancellationToken.None);

			InMemorySink.Instance.Should()
				.HaveMessage("request/").Appearing().Once()
				.WithLevel(LogEventLevel.Information)
					.WithProperty($"request.Version").WithValue(version)
				.And.WithProperty($"request.HttpMethod").WithValue(method.ToString())
				.And.WithProperty($"request.Uri").WithValue(uri)
				.And.WithProperty($"request.Header.{header1}").WithValue(headerValue1)
				.And.WithProperty($"request.Header.{header2}").WithValue(headerValue2)
				.And.WithProperty($"request.Content.Header.Content-Type").WithValue($"{mimeType}; charset={encoding.BodyName}")
#if NET6_0_OR_GREATER
				.And.WithProperty($"request.Content.Header.Content-Length").WithValue(requestContent.Length.ToString())
#endif
				.And.WithProperty($"request.Content").WithValue(requestContent)
				.And.WithProperty($"request.Property.{property1}").WithValue(propertyValue1)
				.And.WithProperty($"request.Property.{property2}").WithValue(propertyValue2)
				;
		}
	}
}
