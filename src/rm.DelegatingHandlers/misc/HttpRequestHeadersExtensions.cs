using System.Net.Http.Headers;
using rm.Extensions;

namespace rm.DelegatingHandlers;

public static class HttpHeadersExtensions
{
	public static bool TryGetValue(this HttpHeaders headers, string name, out string value)
	{
		value = null;
		return headers.TryGetValues(name, out var values)
			&& values.TrySingle(out value);
	}
}
