using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers
{
	public class SwallowingHandler : DelegatingHandler
	{
		private readonly Func<Exception, bool> predicate;

		public SwallowingHandler(
			Func<Exception, bool> predicate)
		{
			this.predicate = predicate
				?? throw new ArgumentNullException(nameof(predicate));
		}

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			try
			{
				return await base.SendAsync(request, cancellationToken)
					.ConfigureAwait(false);
			}
			catch (Exception ex) when (predicate(ex))
			{
				// swallow
				return null!;
			}
		}
	}
}
