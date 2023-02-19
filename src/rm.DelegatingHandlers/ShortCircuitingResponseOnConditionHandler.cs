using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Causes http response for given statusCode, content on condition.
/// </summary>
public class ShortCircuitingResponseOnConditionHandler : DelegatingHandler
{
	private readonly IShortCircuitingResponseOnConditionHandlerSettings shortCircuitingResponseOnConditionHandlerSettings;

	/// <inheritdoc cref="ShortCircuitingResponseOnConditionHandler" />
	public ShortCircuitingResponseOnConditionHandler(
		IShortCircuitingResponseOnConditionHandlerSettings shortCircuitingResponseOnConditionHandlerSettings)
	{
		this.shortCircuitingResponseOnConditionHandlerSettings = shortCircuitingResponseOnConditionHandlerSettings
			?? throw new ArgumentNullException(nameof(shortCircuitingResponseOnConditionHandlerSettings));
	}

	protected override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		bool condition;
		if (request.Properties.TryGetValue(typeof(ShortCircuitingResponseOnConditionHandler).FullName, out var value)
			&& value is not null
			&& (condition = (bool)value)
			&& condition)
		{
			return Task.FromResult(
				new HttpResponseMessage
				{
					StatusCode = shortCircuitingResponseOnConditionHandlerSettings.StatusCode,
					ReasonPhrase = $"{nameof(ShortCircuitingResponseOnConditionHandler)} says hello!",
					Content =
						shortCircuitingResponseOnConditionHandlerSettings.Content == null ?
						null :
						new StringContent(
							shortCircuitingResponseOnConditionHandlerSettings.Content,
							Encoding.UTF8,
							MediaTypeNames.Application.Json),
				});
		}

		return base.SendAsync(request, cancellationToken);
	}
}

public interface IShortCircuitingResponseOnConditionHandlerSettings
{
	HttpStatusCode StatusCode { get; }
	string Content { get; }
}

public record class ShortCircuitingResponseOnConditionHandlerSettings : IShortCircuitingResponseOnConditionHandlerSettings
{
	public HttpStatusCode StatusCode { get; init; }
	public string Content { get; init; }
}
