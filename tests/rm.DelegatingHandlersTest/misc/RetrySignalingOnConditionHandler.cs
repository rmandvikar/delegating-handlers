using System.Net;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

public class RetrySignalingOnConditionHandler : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		HttpResponseMessage response;
		try
		{
			response = await base.SendAsync(request, cancellationToken);
		}
		catch (TaskCanceledException)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			request.Properties[RequestProperties.RetrySignal] = true;
#pragma warning restore CS0618 // Type or member is obsolete
			throw;
		}

		// tweak conditions accordingly
		if (response.StatusCode == (HttpStatusCode)404)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			request.Properties[RequestProperties.RetrySignal] = true;
#pragma warning restore CS0618 // Type or member is obsolete
			return response;
		}
#if NETFRAMEWORK
		if (response.Content != null)
#endif
		{
			var content = await response.Content.ReadAsStringAsync(
#if NET6_0_OR_GREATER
					cancellationToken
#endif
					);
			if (content.Contains("yawn!"))
			{
#pragma warning disable CS0618 // Type or member is obsolete
				request.Properties[RequestProperties.RetrySignal] = true;
#pragma warning restore CS0618 // Type or member is obsolete
				return response;
			}
		}

		return response;
	}
}
