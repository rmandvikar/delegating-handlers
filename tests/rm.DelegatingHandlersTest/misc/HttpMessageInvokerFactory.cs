namespace rm.DelegatingHandlersTest
{
	public static class HttpMessageInvokerFactory
	{
		public static HttpMessageInvoker Create(
			params DelegatingHandler[] handlers)
		{
			return Create(null!, handlers);
		}

		public static HttpMessageInvoker Create(
			HttpMessageHandler innerHandler,
			params DelegatingHandler[] handlers)
		{
			if (handlers == null || !handlers.Any())
			{
				throw new ArgumentNullException(nameof(handlers));
			}
			if (handlers.Any(x => x == null))
			{
				throw new ArgumentNullException(nameof(handlers), "At least one of the handlers is null.");
			}

			var first = handlers[0];

			Array.Reverse(handlers);
			var current = innerHandler;
			foreach (var next in handlers)
			{
				if (current != null)
				{
					next.InnerHandler = current;
				}
				current = next;
			}
			return new HttpMessageInvoker(first);
		}
	}
}
