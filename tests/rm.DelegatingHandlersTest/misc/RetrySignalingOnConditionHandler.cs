using System.Net;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	public class RetrySignalingOnConditionHandler : DelegatingHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			var response = await base.SendAsync(request, cancellationToken);

			// tweak conditions accordingly
			if (response.StatusCode == (HttpStatusCode)404)
			{
#pragma warning disable CS0618 // Type or member is obsolete
				request.Properties[RequestProperties.RetrySignal] = true;
#pragma warning restore CS0618 // Type or member is obsolete
				return response;
			}
			var content = await response.Content.ReadAsStringAsync(cancellationToken);
			if (content.Contains("yawn!"))
			{
#pragma warning disable CS0618 // Type or member is obsolete
				request.Properties[RequestProperties.RetrySignal] = true;
#pragma warning restore CS0618 // Type or member is obsolete
				return response;
			}

			return response;
		}
	}
}
