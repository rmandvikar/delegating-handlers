using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Returns mocked http response.
/// </summary>
public class MockResponseHttpMessageHandler : HttpMessageHandler
{
	private readonly IMockResponseFactory mockResponseFactory;

	/// <inheritdoc cref="MockResponseHttpMessageHandler" />
	public MockResponseHttpMessageHandler(IMockResponseFactory mockResponseFactory)
	{
		this.mockResponseFactory = mockResponseFactory
			?? throw new ArgumentNullException(nameof(mockResponseFactory));
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		return await mockResponseFactory.GetMockResponse(request, cancellationToken)
			.ConfigureAwait(false);
	}
}

public interface IMockResponseFactory
{
	Task<HttpResponseMessage> GetMockResponse(HttpRequestMessage request, CancellationToken cancellationToken);
}
