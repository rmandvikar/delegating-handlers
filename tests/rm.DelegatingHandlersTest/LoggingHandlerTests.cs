using System.Net;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using rm.DelegatingHandlers;
using rm.DelegatingHandlers.Formatting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
using Serilog.Sinks.InMemory;
using Serilog.Sinks.InMemory.Assertions;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class LoggingHandlerTests
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
			var loggingHandler = new LoggingHandler(logger, new LoggingFormatter());

			using var invoker = HttpMessageInvokerFactory.Create(
				loggingHandler, shortCircuitingCannedResponseHandler);

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

		[Test]
		public async Task Logs_Request_Response()
		{
			var logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.WriteTo.InMemory()
				.WriteTo.Console(new JsonFormatter())
				.CreateLogger();
			var version = "2.0";
			var method = HttpMethod.Post;
			var uri = "/health";
			var statusCode = HttpStatusCode.OK;
			var requestContent = "woot?!";
			var responseContent = "woot!!1";
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
			var response =
				new HttpResponseMessage
				{
					Version = new Version(version),
					StatusCode = statusCode,
					Content = new StringContent(responseContent, encoding, mimeType),
				};
			response.Headers.Add(header1, headerValue1);
			response.Headers.Add(header2, headerValue2);

			var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(response);
			var loggingHandler = new LoggingHandler(logger, new LoggingFormatter());

			using var invoker = HttpMessageInvokerFactory.Create(
				loggingHandler, shortCircuitingCannedResponseHandler);

			using var _ = await invoker.SendAsync(request, CancellationToken.None);

			InMemorySink.Instance.Should()
				.HaveMessage("request/response").Appearing().Once()
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
				.And.WithProperty($"response.Version").WithValue(version)
				.And.WithProperty($"response.StatusCode").WithValue((int)statusCode)
				.And.WithProperty($"response.ReasonPhrase").WithValue(statusCode.ToString())
				.And.WithProperty($"response.Header.{header1}").WithValue(headerValue1)
				.And.WithProperty($"response.Header.{header2}").WithValue(headerValue2)
				.And.WithProperty($"response.Content.Header.Content-Type").WithValue($"{mimeType}; charset={encoding.BodyName}")
#if NET6_0_OR_GREATER
				.And.WithProperty($"response.Content.Header.Content-Length").WithValue(responseContent.Length.ToString())
#endif
				.And.WithProperty($"response.Content").WithValue(responseContent)
				.And.WithProperty($"ElapsedMs").WhichValue<long>().Should().BeGreaterThanOrEqualTo(0)
				;
		}

		[Test]
		public async Task Logs_Request_Exception()
		{
			var logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.WriteTo.InMemory()
				.WriteTo.Console(new JsonFormatter())
				.CreateLogger();
			var version = "2.0";
			var method = HttpMethod.Post;
			var uri = "/health";
			var statusCode = HttpStatusCode.OK;
			var requestContent = "woot?!";
			var responseContent = "woot!!1";
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
			var response =
				new HttpResponseMessage
				{
					Version = new Version(version),
					StatusCode = statusCode,
					Content = new StringContent(responseContent, encoding, mimeType),
				};
			response.Headers.Add(header1, headerValue1);
			response.Headers.Add(header2, headerValue2);

			var swallowingHandler = new SwallowingHandler(ex => ex is TurnDownForWhatException);
			var throwingHandler = new ThrowingHandler(new TurnDownForWhatException());
			var loggingHandler = new LoggingHandler(logger, new LoggingFormatter());

			using var invoker = HttpMessageInvokerFactory.Create(
				swallowingHandler, loggingHandler, throwingHandler);

			using var _ = await invoker.SendAsync(request, CancellationToken.None);

			InMemorySink.Instance.Should()
				.HaveMessage("request/exception").Appearing().Once()
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
				.And.WithProperty($"ElapsedMs").WhichValue<long>().Should().BeGreaterThanOrEqualTo(0)
				;
		}

		public class CompactFormatter
		{
			[Test]
			public async Task Logs_Request_Response_Compact()
			{
				var logger = new LoggerConfiguration()
					.MinimumLevel.Verbose()
					.WriteTo.InMemory()
					.WriteTo.Console(new CompactJsonFormatter())
					.CreateLogger();
				var version = "2.0";
				var method = HttpMethod.Post;
				var uri = "/health";
				var statusCode = HttpStatusCode.OK;
				var requestContent = "woot?!";
				var responseContent = "woot!!1";
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
				var response =
					new HttpResponseMessage
					{
						Version = new Version(version),
						StatusCode = statusCode,
						Content = new StringContent(responseContent, encoding, mimeType),
					};
				response.Headers.Add(header1, headerValue1);
				response.Headers.Add(header2, headerValue2);

				var shortCircuitingCannedResponseHandler = new ShortCircuitingCannedResponseHandler(response);
				var loggingHandler = new LoggingHandler(logger, new CompactLoggingFormatter());

				using var invoker = HttpMessageInvokerFactory.Create(
					loggingHandler, shortCircuitingCannedResponseHandler);

				using var _ = await invoker.SendAsync(request, CancellationToken.None);

				InMemorySink.Instance.Should()
					.HaveMessage("request/response").Appearing().Once()
					.WithLevel(LogEventLevel.Information)
						.WithProperty($"rq.v").WithValue(version)
					.And.WithProperty($"rq.m").WithValue(method.ToString())
					.And.WithProperty($"rq.u").WithValue(uri)
					.And.WithProperty($"rq.h.{header1}").WithValue(headerValue1)
					.And.WithProperty($"rq.h.{header2}").WithValue(headerValue2)
					.And.WithProperty($"rq.c.h.Content-Type").WithValue($"{mimeType}; charset={encoding.BodyName}")
#if NET6_0_OR_GREATER
					.And.WithProperty($"rq.c.h.Content-Length").WithValue(requestContent.Length.ToString())
#endif
					.And.WithProperty($"rq.c").WithValue(requestContent)
					.And.WithProperty($"rq.p.{property1}").WithValue(propertyValue1)
					.And.WithProperty($"rq.p.{property2}").WithValue(propertyValue2)
					.And.WithProperty($"rs.v").WithValue(version)
					.And.WithProperty($"rs.s").WithValue((int)statusCode)
					.And.WithProperty($"rs.r").WithValue(statusCode.ToString())
					.And.WithProperty($"rs.h.{header1}").WithValue(headerValue1)
					.And.WithProperty($"rs.h.{header2}").WithValue(headerValue2)
					.And.WithProperty($"rs.c.h.Content-Type").WithValue($"{mimeType}; charset={encoding.BodyName}")
#if NET6_0_OR_GREATER
					.And.WithProperty($"rs.c.h.Content-Length").WithValue(responseContent.Length.ToString())
#endif
					.And.WithProperty($"rs.c").WithValue(responseContent)
					.And.WithProperty($"e").WhichValue<long>().Should().BeGreaterThanOrEqualTo(0)
					;
			}

			[Test]
			public async Task Logs_Request_Exception_Compact()
			{
				var logger = new LoggerConfiguration()
					.MinimumLevel.Verbose()
					.WriteTo.InMemory()
					.WriteTo.Console(new CompactJsonFormatter())
					.CreateLogger();
				var version = "2.0";
				var method = HttpMethod.Post;
				var uri = "/health";
				var statusCode = HttpStatusCode.OK;
				var requestContent = "woot?!";
				var responseContent = "woot!!1";
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
				var response =
					new HttpResponseMessage
					{
						Version = new Version(version),
						StatusCode = statusCode,
						Content = new StringContent(responseContent, encoding, mimeType),
					};
				response.Headers.Add(header1, headerValue1);
				response.Headers.Add(header2, headerValue2);

				var swallowingHandler = new SwallowingHandler(ex => ex is TurnDownForWhatException);
				var throwingHandler = new ThrowingHandler(new TurnDownForWhatException());
				var loggingHandler = new LoggingHandler(logger, new CompactLoggingFormatter());

				using var invoker = HttpMessageInvokerFactory.Create(
					swallowingHandler, loggingHandler, throwingHandler);

				using var _ = await invoker.SendAsync(request, CancellationToken.None);

				InMemorySink.Instance.Should()
					.HaveMessage("request/exception").Appearing().Once()
					.WithLevel(LogEventLevel.Information)
						.WithProperty($"rq.v").WithValue(version)
					.And.WithProperty($"rq.m").WithValue(method.ToString())
					.And.WithProperty($"rq.u").WithValue(uri)
					.And.WithProperty($"rq.h.{header1}").WithValue(headerValue1)
					.And.WithProperty($"rq.h.{header2}").WithValue(headerValue2)
					.And.WithProperty($"rq.c.h.Content-Type").WithValue($"{mimeType}; charset={encoding.BodyName}")
#if NET6_0_OR_GREATER
					.And.WithProperty($"rq.c.h.Content-Length").WithValue(requestContent.Length.ToString())
#endif
					.And.WithProperty($"rq.c").WithValue(requestContent)
					.And.WithProperty($"rq.p.{property1}").WithValue(propertyValue1)
					.And.WithProperty($"rq.p.{property2}").WithValue(propertyValue2)
					.And.WithProperty($"e").WhichValue<long>().Should().BeGreaterThanOrEqualTo(0)
					;
			}
		}
	}
}
