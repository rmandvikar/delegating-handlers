using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace rm.DelegatingHandlers;

/// <summary>
/// Throws an exception on condition.
/// </summary>
public class ThrowingOnConditionHandler : DelegatingHandler
{
	private readonly Exception exception;

	/// <inheritdoc cref="ThrowingOnConditionHandler" />
	public ThrowingOnConditionHandler(
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
		bool condition;
		if (request.Properties.TryGetValue(typeof(ThrowingOnConditionHandler).FullName, out var value)
			&& value is not null
			&& (condition = (bool)value)
			&& condition)
		{
			throw exception;
		}

		return base.SendAsync(request, cancellationToken);
	}
}
