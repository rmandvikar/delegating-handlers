using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Short-circuits with canned actions.
	/// </summary>
	/// <remarks>
	/// Canned action's response could be disposed as it's not meant for multiuse.
	/// </remarks>
	public class ShortCircuitingCannedActionsHandler : DelegatingHandler
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage>[] actions;
		private int iActions = 0;

		private readonly object locker = new object();

		/// <inheritdoc cref="ShortCircuitingCannedActionsHandler" />
		public ShortCircuitingCannedActionsHandler(
			params Func<HttpRequestMessage, HttpResponseMessage>[] actions)
		{
			this.actions = actions
				?? throw new ArgumentNullException(nameof(actions));
		}

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			lock (locker)
			{
				if (iActions < actions.Length)
				{
					return Task.FromResult(actions[iActions++](request));
				}
				return base.SendAsync(request, cancellationToken);
			}
		}
	}
}
