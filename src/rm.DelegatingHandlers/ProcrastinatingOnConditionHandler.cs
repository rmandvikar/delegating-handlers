using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Causes delay on condition to induce http timeouts.
	/// </summary>
	public class ProcrastinatingOnConditionHandler : DelegatingHandler
	{
		private readonly IProcrastinatingOnConditionHandlerSettings procrastinatingOnConditionHandlerSettings;

		/// <inheritdoc cref="ProcrastinatingOnConditionHandler" />
		public ProcrastinatingOnConditionHandler(
			IProcrastinatingOnConditionHandlerSettings procrastinatingOnConditionHandlerSettings)
		{
			this.procrastinatingOnConditionHandlerSettings = procrastinatingOnConditionHandlerSettings
				?? throw new ArgumentNullException(nameof(procrastinatingOnConditionHandlerSettings));
		}

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			bool condition;
			if (request.Properties.TryGetValue(typeof(ProcrastinatingOnConditionHandler).FullName, out var value)
				&& value is not null
				&& (condition = (bool)value)
				&& condition)
			{
				await Task.Delay(procrastinatingOnConditionHandlerSettings.DelayInMilliseconds, cancellationToken)
					.ConfigureAwait(false);
			}

			return await base.SendAsync(request, cancellationToken)
				.ConfigureAwait(false);
		}
	}

	public interface IProcrastinatingOnConditionHandlerSettings
	{
		int DelayInMilliseconds { get; }
	}

	public record class ProcrastinatingOnConditionHandlerSettings : IProcrastinatingOnConditionHandlerSettings
	{
		public int DelayInMilliseconds { get; init; }
	}
}
