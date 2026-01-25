using System.Net.Http;

namespace rm.DelegatingHandlersTest;

public static class HttpMessageInvokerFactory
{
	/// <remarks>
	/// <see href="https://github.com/aspnet/AspNetWebStack/blob/77c4a761eef1ffd2944041f903257169b933fb32/src/System.Net.Http.Formatting/HttpClientFactory.cs#L13-L27">source</see>
	/// </remarks>
	public static HttpMessageInvoker Create(
		params DelegatingHandler[] handlers)
	{
		// it is ok for innerHandler arg to be null
		return Create(null!, handlers);
	}

	/// <remarks>
	/// <see href="https://github.com/aspnet/AspNetWebStack/blob/77c4a761eef1ffd2944041f903257169b933fb32/src/System.Net.Http.Formatting/HttpClientFactory.cs#L47-L90">source</see>
	/// </remarks>
	public static HttpMessageInvoker Create(
		HttpMessageHandler innerHandler,
		params DelegatingHandler[] handlers)
	{
		if (innerHandler == null && (handlers == null || !handlers.Any()))
		{
			throw new ArgumentNullException($"{nameof(innerHandler)}, {nameof(handlers)}", "Both innerHandler and handlers are null/empty.");
		}
		if (innerHandler != null && (handlers == null || !handlers.Any()))
		{
			return new HttpMessageInvoker(innerHandler);
		}
		if (handlers.Any(x => x == null))
		{
			throw new ArgumentNullException(nameof(handlers), "At least one of the handlers is null.");
		}

		var firstHandler = handlers[0];

		var reversedHandlers = handlers.AsEnumerable().Reverse();
		// it is ok for innerHandler arg to be null
		var nextHandler = innerHandler!;
		foreach (var currentHandler in reversedHandlers)
		{
			// see https://github.com/microsoft/referencesource/blob/ec9fa9ae770d522a5b5f0607898044b7478574a3/System/net/System/Net/Http/DelegatingHandler.cs#L21-L38
			if (nextHandler != null)
			{
				// do not throw if currentHandler.InnerHandler is not null, simply overwrite it
				currentHandler.InnerHandler = nextHandler;
			}
			nextHandler = currentHandler;
		}
		return new HttpMessageInvoker(firstHandler);
	}
}
