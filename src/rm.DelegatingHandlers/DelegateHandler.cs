using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlersTest
{
	/// <summary>
	/// Runs delegates.
	/// </summary>
	public class DelegateHandler : DelegatingHandler
	{
		private readonly Func<HttpRequestMessage, CancellationToken, Task> preDelegate;
		private readonly Func<HttpRequestMessage, HttpResponseMessage, CancellationToken, Task> postDelegate;

		/// <inheritdoc cref="DelegateHandler" />
		public DelegateHandler(
			Func<HttpRequestMessage, CancellationToken, Task> preDelegate = null!,
			Func<HttpRequestMessage, HttpResponseMessage, CancellationToken, Task> postDelegate = null!)
		{
			// note: for flexibility, don't throw if both are null
			this.preDelegate = preDelegate!;
			this.postDelegate = postDelegate!;
		}

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			if (preDelegate != null)
			{
				await preDelegate(request, cancellationToken)
					.ConfigureAwait(false);
			}

			var response = await base.SendAsync(request, cancellationToken);

			if (postDelegate != null)
			{
				await postDelegate(request, response, cancellationToken)
					.ConfigureAwait(false);
			}

			return response;
		}
	}
}
