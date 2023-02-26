using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Serilog.Core;

namespace rm.DelegatingHandlers.Formatting;

/// <inheritdoc cref="ILoggingFormatter" />
public class LoggingFormatter : ILoggingFormatter
{
	private readonly LoggingFormatterHelper loggingFormatterHelper = new LoggingFormatterHelper();

	private const string requestPrefix = "request";
	private const string responsePrefix = "response";

	public ILogEventEnricher FormatRequestVersion(Version version)
	{
		return loggingFormatterHelper.FormatRequestVersion(version, $"{requestPrefix}.Version");
	}

	public ILogEventEnricher FormatRequestHttpMethod(HttpMethod method)
	{
		return loggingFormatterHelper.FormatRequestHttpMethod(method, $"{requestPrefix}.HttpMethod");
	}

	public ILogEventEnricher FormatRequestUri(Uri uri)
	{
		return loggingFormatterHelper.FormatRequestUri(uri, $"{requestPrefix}.Uri");
	}

	public IEnumerable<ILogEventEnricher> FormatRequestHeaders(HttpRequestHeaders requestHeaders)
	{
		return loggingFormatterHelper.FormatRequestHeaders(requestHeaders, $"{requestPrefix}.Header");
	}

	public ILogEventEnricher FormatRequestContent(string content)
	{
		return loggingFormatterHelper.FormatRequestContent(content, $"{requestPrefix}.Content");
	}

	public IEnumerable<ILogEventEnricher> FormatRequestContentHeaders(HttpContentHeaders contentHeaders)
	{
		return loggingFormatterHelper.FormatRequestContentHeaders(contentHeaders, $"{requestPrefix}.Content.Header");
	}

	public IEnumerable<ILogEventEnricher> FormatProperties(IDictionary<string, object> properties)
	{
		return loggingFormatterHelper.FormatProperties(properties, $"{requestPrefix}.Property");
	}

	public ILogEventEnricher FormatResponseVersion(Version version)
	{
		return loggingFormatterHelper.FormatResponseVersion(version, $"{responsePrefix}.Version");
	}

	public ILogEventEnricher FormatResponseStatuscode(HttpStatusCode httpStatusCode)
	{
		return loggingFormatterHelper.FormatResponseStatuscode(httpStatusCode, $"{responsePrefix}.StatusCode");
	}

	public ILogEventEnricher FormatResponseReasonPhrase(string reasonPhrase)
	{
		return loggingFormatterHelper.FormatResponseReasonPhrase(reasonPhrase, $"{responsePrefix}.ReasonPhrase");
	}

	public IEnumerable<ILogEventEnricher> FormatResponseHeaders(HttpResponseHeaders responseHeaders)
	{
		return loggingFormatterHelper.FormatResponseHeaders(responseHeaders, $"{responsePrefix}.Header");
	}

	public ILogEventEnricher FormatResponseContent(string content)
	{
		return loggingFormatterHelper.FormatResponseContent(content, $"{responsePrefix}.Content");
	}

	public IEnumerable<ILogEventEnricher> FormatResponseContentHeaders(HttpContentHeaders contentHeaders)
	{
		return loggingFormatterHelper.FormatRequestContentHeaders(contentHeaders, $"{responsePrefix}.Content.Header");
	}

#if NETSTANDARD2_1
	public IEnumerable<ILogEventEnricher> FormatResponseTrailingHeaders(HttpResponseHeaders responseTrailingHeaders)
	{
		return loggingFormatterHelper.FormatResponseTrailingHeaders(responseTrailingHeaders, $"{responsePrefix}.TrailingHeader");
	}
#endif

	public ILogEventEnricher FormatElapsed(Stopwatch stopwatch)
	{
		return loggingFormatterHelper.FormatElapsed(stopwatch, "ElapsedMs");
	}

	public IEnumerable<ILogEventEnricher> FormatException(Exception exception)
	{
		return loggingFormatterHelper.FormatException(exception);
	}
}
