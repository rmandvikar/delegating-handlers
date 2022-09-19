using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using rm.DelegatingHandlers.Formatting;
using Serilog;
using Serilog.Core;

namespace rm.DelegatingHandlers;

public static class LoggerHttpExtensions
{
	private const int defaultCapacity = 20;

	public static async Task<ILogger> ForContextAsync(
		this ILogger logger,
		HttpRequestMessage request,
		ILoggingFormatter loggingFormatter)
	{
		var enrichers = new List<ILogEventEnricher>(capacity: defaultCapacity);
		enrichers.AddRange(
			new[]
			{
				loggingFormatter.FormatRequestVersion(request.Version),
				loggingFormatter.FormatRequestHttpMethod(request.Method),
				loggingFormatter.FormatRequestUri(request.RequestUri),
			});
		enrichers.AddRange(loggingFormatter.FormatRequestHeaders(request.Headers));
		if (request.Content != null)
		{
			var content = await request.Content.ReadAsStringAsync()
				.ConfigureAwait(false);
			enrichers.Add(loggingFormatter.FormatRequestContent(content));
			// content headers could change once content is read
			enrichers.AddRange(loggingFormatter.FormatRequestContentHeaders(request.Content.Headers));
		}
		enrichers.AddRange(loggingFormatter.FormatProperties(request.Properties));
		return logger.ForContext(enrichers);
	}

	public static async Task<ILogger> ForContextAsync(
		this ILogger logger,
		HttpResponseMessage response,
		ILoggingFormatter loggingFormatter)
	{
		var enrichers = new List<ILogEventEnricher>(capacity: defaultCapacity);
		enrichers.AddRange(
			new[]
			{
				loggingFormatter.FormatResponseVersion(response.Version),
				loggingFormatter.FormatResponseStatuscode(response.StatusCode),
				loggingFormatter.FormatResponseReasonPhrase(response.ReasonPhrase),
			});
		enrichers.AddRange(loggingFormatter.FormatResponseHeaders(response.Headers));
		if (response.Content != null)
		{
			var content = await response.Content.ReadAsStringAsync()
				.ConfigureAwait(false);
			enrichers.Add(loggingFormatter.FormatResponseContent(content));
			// content headers could change once content is read
			enrichers.AddRange(loggingFormatter.FormatResponseContentHeaders(response.Content.Headers));
		}
#if NETSTANDARD2_1
		enrichers.AddRange(loggingFormatter.FormatResponseTrailingHeaders(response.TrailingHeaders));
#endif
		return logger.ForContext(enrichers);
	}

	public static ILogger ForContext(
		this ILogger logger,
		Stopwatch stopwatch,
		ILoggingFormatter loggingFormatter)
	{
		return logger.ForContext(loggingFormatter.FormatElapsed(stopwatch));
	}

	public static ILogger ForContext(
		this ILogger logger,
		Exception exception,
		ILoggingFormatter loggingFormatter)
	{
		return logger.ForContext(loggingFormatter.FormatException(exception));
	}

	public static string ToCsv(
		this IEnumerable<string> source)
	{
		return string.Join(", ", source);
	}
}
