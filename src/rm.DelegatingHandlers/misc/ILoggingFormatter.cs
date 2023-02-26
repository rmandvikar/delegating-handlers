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
/// into serilog enrichers.
/// </summary>
public interface ILoggingFormatter
{
	ILogEventEnricher FormatRequestVersion(Version version);
	ILogEventEnricher FormatRequestHttpMethod(HttpMethod method);
	ILogEventEnricher FormatRequestUri(Uri uri);
	IEnumerable<ILogEventEnricher> FormatRequestHeaders(HttpRequestHeaders headers);
	ILogEventEnricher FormatRequestContent(string content);
	IEnumerable<ILogEventEnricher> FormatRequestContentHeaders(HttpContentHeaders headers);
	IEnumerable<ILogEventEnricher> FormatProperties(IDictionary<string, object> properties);

	ILogEventEnricher FormatResponseVersion(Version version);
	ILogEventEnricher FormatResponseStatuscode(HttpStatusCode httpStatusCode);
	ILogEventEnricher FormatResponseReasonPhrase(string reasonPhrase);
	IEnumerable<ILogEventEnricher> FormatResponseHeaders(HttpResponseHeaders headers);
	ILogEventEnricher FormatResponseContent(string content);
	IEnumerable<ILogEventEnricher> FormatResponseContentHeaders(HttpContentHeaders headers);
#if NETSTANDARD2_1
	IEnumerable<ILogEventEnricher> FormatResponseTrailingHeaders(HttpResponseHeaders headers);
#endif

	ILogEventEnricher FormatElapsed(Stopwatch stopwatch);
	IEnumerable<ILogEventEnricher> FormatException(Exception exception);
}
