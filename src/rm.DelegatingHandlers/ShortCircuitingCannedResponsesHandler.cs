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
	/// Canned response could be disposed as it's not meant for multiuse.
	/// </remarks>
	public class ShortCircuitingCannedResponsesHandler : DelegatingHandler
	{
		private readonly HttpResponseMessage[] responses;
		private int iResponses = 0;

		private readonly object locker = new object();

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
			lock (locker)
			{
				if (iResponses == responses.Length)
				{
					throw new InvalidOperationException($"Exhausted {responses.Length} response(s)");
				}
				return Task.FromResult(responses[iResponses++]);
			}
		}
	}
}
