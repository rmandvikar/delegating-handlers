using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Causes delay to induce http timeouts.
	/// </summary>
	public class ProcrastinatingHandler : DelegatingHandler
	{
		private readonly IProcrastinatingHandlerSettings procrastinatingHandlerSettings;

		/// <inheritdoc cref="ProcrastinatingHandler" />
		public ProcrastinatingHandler(
			IProcrastinatingHandlerSettings procrastinatingHandlerSettings)
		{
			this.procrastinatingHandlerSettings = procrastinatingHandlerSettings
				?? throw new ArgumentNullException(nameof(procrastinatingHandlerSettings));
		}

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			await Task.Delay(procrastinatingHandlerSettings.DelayInMilliseconds, cancellationToken)
				.ConfigureAwait(false);

			return await base.SendAsync(request, cancellationToken)
				.ConfigureAwait(false);
		}
	}

	public interface IProcrastinatingHandlerSettings
	{
		int DelayInMilliseconds { get; }
	}

	public record class ProcrastinatingHandlerSettings : IProcrastinatingHandlerSettings
	{
		public int DelayInMilliseconds { get; init; }
	}
}
