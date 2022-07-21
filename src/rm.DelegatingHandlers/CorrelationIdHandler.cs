using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using rm.Extensions;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Adds correlationId value from <see cref="ICorrelationIdContext"/> to request.
	/// </summary>
	public class CorrelationIdHandler : DelegatingHandler
	{
		private readonly ICorrelationIdContext correlationIdContext;

		/// <inheritdoc cref="CorrelationIdHandler" />
		public CorrelationIdHandler(
			ICorrelationIdContext correlationIdContext)
		{
			this.correlationIdContext = correlationIdContext
				?? throw new ArgumentNullException(nameof(correlationIdContext));
		}

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			// correlationId value must always come from correlationIdContext
			if (request.Headers.TryGetValue(RequestHeaders.CorrelationId, out var value))
			{
				throw new InvalidOperationException($"{RequestHeaders.CorrelationId} header already present with value '{value}'");
			}

			var correlationId = correlationIdContext.GetValue();
			if (correlationId.IsNullOrWhiteSpace())
			{
				throw new InvalidOperationException($"{RequestHeaders.CorrelationId} header value cannot be null/empty/whitespace");
			}

			request.Headers.Add(RequestHeaders.CorrelationId, correlationId);

			var response = await base.SendAsync(request, cancellationToken)
				.ConfigureAwait(false);

			// add correlationId to response header
			// if correlationId already present, noop
			// assume valid correlationId value if already present
			if (!response.Headers.Contains(ResponseHeaders.CorrelationId))
			{
				response.Headers.Add(ResponseHeaders.CorrelationId, correlationId);
			}

			return response;
		}
	}

	/// <summary>
	/// Defines correlationId context.
	/// </summary>
	public interface ICorrelationIdContext
	{
		/// <summary>
		/// Returns correlationId value.
		/// </summary>
		string GetValue();
	}
}
