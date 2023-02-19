using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

public class FaultOnConditionHandler : DelegatingHandler
{
	protected override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		// set unconditionally for test, but this can be set conditionally
#pragma warning disable CS0618 // Type or member is obsolete
		request.Properties[typeof(FaultWindowSignalingOnConditionHandler).FullName!] = true;
#pragma warning restore CS0618 // Type or member is obsolete

		return base.SendAsync(request, cancellationToken);
	}
}
