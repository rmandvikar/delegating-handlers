using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace rm.DelegatingHandlers.Formatting;

/// <summary>
/// Helper class for <see cref="LoggingFormatter"/>.
/// </summary>
internal class LoggingFormatterHelper
{
	internal ILogEventEnricher FormatRequestVersion(Version version, string name)
	{
		return new PropertyEnricher(name, version);
	}

	internal ILogEventEnricher FormatRequestHttpMethod(HttpMethod method, string name)
	{
		return new PropertyEnricher(name, method);
	}

	internal ILogEventEnricher FormatRequestUri(Uri uri, string name)
	{
		return new PropertyEnricher(name, uri);
	}

	internal IEnumerable<ILogEventEnricher> FormatRequestHeaders(HttpRequestHeaders headers, string prefix)
	{
		foreach (var header in headers)
		{
			// header value is IEnumerable
			yield return new PropertyEnricher($"{prefix}.{header.Key}", header.Value.ToCsv());
		}
	}

	internal ILogEventEnricher FormatRequestContent(string content, string name)
	{
		return new PropertyEnricher(name, content);
	}

	internal IEnumerable<ILogEventEnricher> FormatRequestContentHeaders(HttpContentHeaders contentHeaders, string prefix)
	{
		foreach (var contentHeader in contentHeaders)
		{
			// header value is IEnumerable
			yield return new PropertyEnricher($"{prefix}.{contentHeader.Key}", contentHeader.Value.ToCsv());
		}
	}

	internal IEnumerable<ILogEventEnricher> FormatProperties(IDictionary<string, object> properties, string prefix)
	{
		foreach (var property in properties)
		{
			yield return new PropertyEnricher($"{prefix}.{property.Key}", property.Value);
		}
	}

	internal ILogEventEnricher FormatResponseVersion(Version version, string name)
	{
		return new PropertyEnricher(name, version);
	}

	internal ILogEventEnricher FormatResponseStatuscode(HttpStatusCode httpStatusCode, string name)
	{
		return new PropertyEnricher(name, (int)httpStatusCode);
	}

	internal ILogEventEnricher FormatResponseReasonPhrase(string reasonPhrase, string name)
	{
		return new PropertyEnricher(name, reasonPhrase);
	}

	internal IEnumerable<ILogEventEnricher> FormatResponseHeaders(HttpResponseHeaders headers, string prefix)
	{
		foreach (var header in headers)
		{
			// header value is IEnumerable
			yield return new PropertyEnricher($"{prefix}.{header.Key}", header.Value.ToCsv());
		}
	}

	internal ILogEventEnricher FormatResponseContent(string content, string name)
	{
		return new PropertyEnricher(name, content);
	}

	internal IEnumerable<ILogEventEnricher> FormatResponseContentHeaders(HttpContentHeaders contentHeaders, string prefix)
	{
		foreach (var contentHeader in contentHeaders)
		{
			// header value is IEnumerable
			yield return new PropertyEnricher($"{prefix}.{contentHeader.Key}", contentHeader.Value.ToCsv());
		}
	}

	internal ILogEventEnricher FormatElapsed(Stopwatch stopwatch, string name)
	{
		return new PropertyEnricher(name, stopwatch.ElapsedMilliseconds);
	}

	internal IEnumerable<ILogEventEnricher> FormatException(Exception exception)
	{
		// enrich ex properties, if any
		return Enumerable.Empty<ILogEventEnricher>();
	}
}
