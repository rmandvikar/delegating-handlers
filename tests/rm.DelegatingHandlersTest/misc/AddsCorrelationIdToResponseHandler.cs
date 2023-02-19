using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

public class AddsCorrelationIdToResponseHandler : DelegatingHandler
{
	private readonly string value;

	public AddsCorrelationIdToResponseHandler(string value)
	{
		this.value = value;
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var response = await base.SendAsync(request, cancellationToken);
		response.Headers.Add(ResponseHeaders.CorrelationId, value);
		return response;
	}
}
