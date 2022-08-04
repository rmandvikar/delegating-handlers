using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Short-circuits with canned http responses.
	/// </summary>
	/// <remarks>
	/// Canned response should not be disposed if used with a retry handler
	/// as it's meant for multiuse. If so, consider using <see cref="ShortCircuitingResponseHandler"/>
	/// instead.
	/// </remarks>
	public class ShortCircuitingCannedResponsesHandler : DelegatingHandler
	{
		private readonly HttpResponseMessage[] responses;
		private int iResponses = 0;

		/// <inheritdoc cref="ShortCircuitingCannedResponsesHandler" />
		public ShortCircuitingCannedResponsesHandler(
			params HttpResponseMessage[] responses)
		{
			this.responses = responses
				?? throw new ArgumentNullException(nameof(responses));
		}

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			return Task.FromResult(responses[iResponses++]);
		}
	}
}
