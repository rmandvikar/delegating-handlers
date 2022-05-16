using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Simply relays request, just for fun.
	/// </summary>
	public class RelayHandler : DelegatingHandler
	{
		/// <inheritdoc cref="RelayHandler" />
		public RelayHandler()
		{ }

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			return base.SendAsync(request, cancellationToken);
		}
	}
}
