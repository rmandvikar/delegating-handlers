using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Throws an exception.
	/// </summary>
	public class ThrowingHandler : DelegatingHandler
	{
		private readonly Exception exception;

		/// <inheritdoc cref="ThrowingHandler" />
		public ThrowingHandler(
			Exception exception)
		{
			// funny, no?
			this.exception = exception
				?? throw new ArgumentNullException(nameof(exception));
		}

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			throw exception;
		}
	}
}
