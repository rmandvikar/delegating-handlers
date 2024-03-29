﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using rm.Extensions;

namespace rm.DelegatingHandlers;

/// <summary>
/// Adds correlationId value from <see cref="ICorrelationIdContext"/> to request.
/// </summary>
public class CorrelationIdHandler : DelegatingHandler
{
	private readonly ICorrelationIdContext correlationIdContext;

	/// <inheritdoc cref="CorrelationIdHandler" />
	public CorrelationIdHandler(
		ICorrelationIdContext correlationIdContext)
	{
		this.correlationIdContext = correlationIdContext
			?? throw new ArgumentNullException(nameof(correlationIdContext));
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		// correlationId value must always come from correlationIdContext
		if (request.Headers.TryGetValue(RequestHeaders.CorrelationId, out var value))
		{
			throw new InvalidOperationException($"{RequestHeaders.CorrelationId} header already present with value '{value}'");
		}

		var correlationIds = correlationIdContext.GetValue();

		if (correlationIds.Any(c => c.IsNullOrWhiteSpace()))
		{
			throw new InvalidOperationException($"{RequestHeaders.CorrelationId} header values cannot contain null/empty/whitespace value");
		}
		if (correlationIds.IsNullOrEmpty())
		{
			throw new InvalidOperationException($"{RequestHeaders.CorrelationId} header value cannot be null/empty/whitespace");
		}

		foreach (var correlationId in correlationIds)
		{
			request.Headers.Add(RequestHeaders.CorrelationId, correlationId);
		}

		var response = await base.SendAsync(request, cancellationToken)
			.ConfigureAwait(false);

		// add correlationId(s) to response header
		// if correlationId(s) already present, dedupe but can't guarantee order
		response.Headers.TryGetValues(ResponseHeaders.CorrelationId, out var responseCorrelationIds);
		responseCorrelationIds = responseCorrelationIds.EmptyIfDefault();
		foreach (var correlationId in correlationIds)
		{
			if (!responseCorrelationIds.Any(responseCorrelationId => responseCorrelationId == correlationId))
			{
				response.Headers.Add(ResponseHeaders.CorrelationId, correlationId);
			}
		}

		return response;
	}
}

/// <summary>
/// Defines correlationId context.
/// </summary>
public interface ICorrelationIdContext
{
	/// <summary>
	/// Returns correlationId value.
	/// </summary>
	StringValues GetValue();
}
