namespace rm.DelegatingHandlersTest
{
	public class SwallowingHandler : DelegatingHandler
	{
		private readonly Func<Exception, bool> predicate;

		public SwallowingHandler(Func<Exception, bool> predicate)
		{
			this.predicate = predicate
				?? throw new ArgumentNullException(nameof(predicate));
		}

		protected async override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			try
			{
				return await base.SendAsync(request, cancellationToken)
					.ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				// swallow
				if (predicate(ex))
				{
					return null!;
				}
				throw;
			}
		}
	}
}
