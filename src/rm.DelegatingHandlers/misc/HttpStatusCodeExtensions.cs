using System.Net;

namespace rm.DelegatingHandlers;

public static class HttpStatusCodeExtensions
{
	public static bool Is1xx(this HttpStatusCode statusCode)
	{
		return GetStatusCodeClassDigit(statusCode) == 1;
	}

	public static bool Is2xx(this HttpStatusCode statusCode)
	{
		return GetStatusCodeClassDigit(statusCode) == 2;
	}

	public static bool Is3xx(this HttpStatusCode statusCode)
	{
		return GetStatusCodeClassDigit(statusCode) == 3;
	}

	public static bool Is4xx(this HttpStatusCode statusCode)
	{
		return GetStatusCodeClassDigit(statusCode) == 4;
	}

	public static bool Is5xx(this HttpStatusCode statusCode)
	{
		return GetStatusCodeClassDigit(statusCode) == 5;
	}

	// https://en.wikipedia.org/wiki/List_of_HTTP_status_codes
	private static int GetStatusCodeClassDigit(HttpStatusCode statusCode)
	{
		return (int)statusCode / 100;
	}

	/// <summary>
	/// Returns true if status code is a client error status code (4xx).
	/// </summary>
	public static bool IsClientErrorStatusCode(this HttpStatusCode statusCode)
	{
		return statusCode.Is4xx();
	}

	/// <summary>
	/// Returns true if status code is a server error status code (5xx).
	/// </summary>
	public static bool IsServerErrorStatusCode(this HttpStatusCode statusCode)
	{
		return statusCode.Is5xx();
	}

	/// <summary>
	/// Returns true if status code is an error status code (4xx, 5xx).
	/// </summary>
	public static bool IsErrorStatusCode(this HttpStatusCode statusCode)
	{
		return statusCode.Is4xx() || statusCode.Is5xx();
	}
}
