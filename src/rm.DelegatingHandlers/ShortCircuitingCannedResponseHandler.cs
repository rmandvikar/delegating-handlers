using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Short-circuits with a canned http response.
	/// </summary>
	/// <remarks>
	/// Canned response should not be disposed if used with a retry handler
	/// as it's meant for multiuse. If so, consider using <see cref="ShortCircuitingResponseHandler"/>
	/// instead.
	/// </remarks>
	public class ShortCircuitingCannedResponseHandler : DelegatingHandler
	{
		private readonly HttpResponseMessage response;

		/// <inheritdoc cref="ShortCircuitingCannedResponseHandler" />
		public ShortCircuitingCannedResponseHandler(
			HttpResponseMessage response)
		{
			this.response = response
				?? throw new ArgumentNullException(nameof(response));
		}

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			return Task.FromResult(response);
		}
	}
}
