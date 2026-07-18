using System.Net.Http;

namespace rm.DelegatingHandlersTest;

internal static class HttpMethodItems
{
	public static IEnumerable<HttpMethod> HttpMethods()
	{
		yield return HttpMethod.Get;
		yield return HttpMethod.Put;
		yield return HttpMethod.Post;
		yield return HttpMethod.Delete;
		yield return HttpMethod.Head;

		yield return HttpMethod.Options;

#if NET10_0_OR_GREATER
		yield return HttpMethod.Query;
#endif
	}
}
