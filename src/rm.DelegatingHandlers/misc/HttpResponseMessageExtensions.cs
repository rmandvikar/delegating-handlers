using System.Net.Http;

namespace rm.DelegatingHandlers;

public static class HttpResponseMessageExtensions
{
	public static bool Is1xx(this HttpResponseMessage httpResponseMessage)
	{
		return httpResponseMessage.StatusCode.Is1xx();
	}

	public static bool Is2xx(this HttpResponseMessage httpResponseMessage)
	{
		return httpResponseMessage.StatusCode.Is2xx();
	}

	public static bool Is3xx(this HttpResponseMessage httpResponseMessage)
	{
		return httpResponseMessage.StatusCode.Is3xx();
	}

	public static bool Is4xx(this HttpResponseMessage httpResponseMessage)
	{
		return httpResponseMessage.StatusCode.Is4xx();
	}

	public static bool Is5xx(this HttpResponseMessage httpResponseMessage)
	{
		return httpResponseMessage.StatusCode.Is5xx();
	}

	/// <summary>
	/// Returns true if status code is a client error status code (4xx).
	/// </summary>
	public static bool IsClientErrorStatusCode(this HttpResponseMessage httpResponseMessage)
	{
		return httpResponseMessage.StatusCode.Is4xx();
	}

	/// <summary>
	/// Returns true if status code is a server error status code (5xx).
	/// </summary>
	public static bool IsServerErrorStatusCode(this HttpResponseMessage httpResponseMessage)
	{
		return httpResponseMessage.StatusCode.Is5xx();
	}

	/// <summary>
	/// Returns true if status code is an error status code (4xx, 5xx).
	/// </summary>
	public static bool IsErrorStatusCode(this HttpResponseMessage httpResponseMessage)
	{
		return httpResponseMessage.StatusCode.Is4xx() || httpResponseMessage.StatusCode.Is5xx();
	}
}
