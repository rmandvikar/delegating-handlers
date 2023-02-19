using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Returns null http response.
/// </summary>
/// <remarks>
/// It's a design principle for handler to return non-null HttpResponseMessage
/// so this behavior is not possible in app code, but it's helpful for unit tests
/// (<see href="https://github.com/dotnet/runtime/issues/19925">source</see>).
/// </remarks>
public class NullResponseHandler : DelegatingHandler
{
	/// <inheritdoc cref="NullResponseHandler" />
	public NullResponseHandler()
	{ }

	protected override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		return Task.FromResult((HttpResponseMessage)null);
	}
}
