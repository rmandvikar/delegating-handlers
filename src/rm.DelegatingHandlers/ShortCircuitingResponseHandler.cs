using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Causes http response for given statusCode, content.
	/// </summary>
	public class ShortCircuitingResponseHandler : DelegatingHandler
	{
		private readonly IShortCircuitingResponseHandlerSettings shortCircuitingResponseHandlerSettings;

		/// <inheritdoc cref="ShortCircuitingResponseHandler" />
		public ShortCircuitingResponseHandler(
			IShortCircuitingResponseHandlerSettings shortCircuitingResponseHandlerSettings)
		{
			this.shortCircuitingResponseHandlerSettings = shortCircuitingResponseHandlerSettings
				?? throw new ArgumentNullException(nameof(shortCircuitingResponseHandlerSettings));
		}

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			return Task.FromResult(
				new HttpResponseMessage
				{
					StatusCode = shortCircuitingResponseHandlerSettings.StatusCode,
					ReasonPhrase = $"{nameof(ShortCircuitingResponseHandler)} says hello!",
					Content =
						shortCircuitingResponseHandlerSettings.Content == null ?
						null :
						new StringContent(
							shortCircuitingResponseHandlerSettings.Content,
							Encoding.UTF8,
							MediaTypeNames.Application.Json),
				});
		}
	}

	public interface IShortCircuitingResponseHandlerSettings
	{
		HttpStatusCode StatusCode { get; }
		string Content { get; }
	}

	public record class ShortCircuitingResponseHandlerSettings : IShortCircuitingResponseHandlerSettings
	{
		public HttpStatusCode StatusCode { get; init; }
		public string Content { get; init; }
	}
}
