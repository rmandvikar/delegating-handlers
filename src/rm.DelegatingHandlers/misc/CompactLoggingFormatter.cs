using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Serilog.Core;

namespace rm.DelegatingHandlers.Formatting;

/// <summary>
/// Formats <see cref="HttpRequestMessage"/>, and <see cref="HttpResponseMessage"/> properties
/// in a compact way into serilog enrichers.
/// </summary>
public class CompactLoggingFormatter : ILoggingFormatter
{
	private readonly LoggingFormatterHelper loggingFormatterHelper = new LoggingFormatterHelper();

	private const string requestPrefix = "rq";
	private const string responsePrefix = "rs";

	public ILogEventEnricher FormatRequestVersion(Version version)
	{
		return loggingFormatterHelper.FormatRequestVersion(version, $"{requestPrefix}.v");
	}

	public ILogEventEnricher FormatRequestHttpMethod(HttpMethod method)
	{
		return loggingFormatterHelper.FormatRequestHttpMethod(method, $"{requestPrefix}.m");
	}

	public ILogEventEnricher FormatRequestUri(Uri uri)
	{
		return loggingFormatterHelper.FormatRequestUri(uri, $"{requestPrefix}.u");
	}

	public IEnumerable<ILogEventEnricher> FormatRequestHeaders(HttpRequestHeaders requestHeaders)
	{
		return loggingFormatterHelper.FormatRequestHeaders(requestHeaders, $"{requestPrefix}.h");
	}

	public IEnumerable<ILogEventEnricher> FormatRequestContentHeaders(HttpContentHeaders contentHeaders)
	{
		return loggingFormatterHelper.FormatRequestContentHeaders(contentHeaders, $"{requestPrefix}.c.h");
	}

	public ILogEventEnricher FormatRequestContent(string content)
	{
		return loggingFormatterHelper.FormatRequestContent(content, $"{requestPrefix}.c");
	}

	public IEnumerable<ILogEventEnricher> FormatProperties(IDictionary<string, object> properties)
	{
		return loggingFormatterHelper.FormatProperties(properties, $"{requestPrefix}.p");
	}

	public ILogEventEnricher FormatResponseVersion(Version version)
	{
		return loggingFormatterHelper.FormatResponseVersion(version, $"{responsePrefix}.v");
	}

	public ILogEventEnricher FormatResponseStatuscode(HttpStatusCode httpStatusCode)
	{
		return loggingFormatterHelper.FormatResponseStatuscode(httpStatusCode, $"{responsePrefix}.s");
	}

	public ILogEventEnricher FormatResponseReasonPhrase(string reasonPhrase)
	{
		return loggingFormatterHelper.FormatResponseReasonPhrase(reasonPhrase, $"{responsePrefix}.r");
	}

	public IEnumerable<ILogEventEnricher> FormatResponseHeaders(HttpResponseHeaders responseHeaders)
	{
		return loggingFormatterHelper.FormatResponseHeaders(responseHeaders, $"{responsePrefix}.h");
	}

	public ILogEventEnricher FormatResponseContent(string content)
	{
		return loggingFormatterHelper.FormatResponseContent(content, $"{responsePrefix}.c");
	}

	public IEnumerable<ILogEventEnricher> FormatResponseContentHeaders(HttpContentHeaders contentHeaders)
	{
		return loggingFormatterHelper.FormatResponseContentHeaders(contentHeaders, $"{responsePrefix}.c.h");
	}

#if NETSTANDARD2_1
	public IEnumerable<ILogEventEnricher> FormatResponseTrailingHeaders(HttpResponseHeaders responseTrailingHeaders)
	{
		return loggingFormatterHelper.FormatResponseTrailingHeaders(responseTrailingHeaders, $"{responsePrefix}.th");
	}
#endif

	public ILogEventEnricher FormatElapsed(Stopwatch stopwatch)
	{
		return loggingFormatterHelper.FormatElapsed(stopwatch, "e");
	}

	public IEnumerable<ILogEventEnricher> FormatException(Exception exception)
	{
		return loggingFormatterHelper.FormatException(exception);
	}
}
