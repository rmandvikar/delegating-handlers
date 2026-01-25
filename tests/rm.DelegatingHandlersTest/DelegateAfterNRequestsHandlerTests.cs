using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class DelegateAfterNRequestsHandlerTests
{
	[Test]
	public async Task Runs_PreDelegate_PostDelegate()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var n = 5;
		var preDelegateExecutedCount = 0;
		var postDelegateExecutedCount = 0;
		var delegateAfterNRequestsHandler = new DelegateAfterNRequestsHandler(
			new DelegateAfterNRequestsHandlerSettings
			{
				N = n,
				PreDelegate = async (request, ct) =>
				{
					preDelegateExecutedCount++;
				},
				PostDelegate = async (request, response, ct) =>
				{
					postDelegateExecutedCount++;
				},
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), delegateAfterNRequestsHandler);

		for (int i = 0; i < n; i++)
		{
			using var _ = await invoker.SendAsync(fixture.Create<HttpRequestMessage>(), CancellationToken.None);
		}
		using var _1 = await invoker.SendAsync(fixture.Create<HttpRequestMessage>(), CancellationToken.None);

		Assert.AreEqual(1, preDelegateExecutedCount);
		Assert.AreEqual(1, postDelegateExecutedCount);
	}

	[Test]
	public async Task Runs_PreDelegate_PostDelegate_After()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var n = 5;
		var preDelegateExecutedCount = 0;
		var postDelegateExecutedCount = 0;
		var delegateAfterNRequestsHandler = new DelegateAfterNRequestsHandler(
			new DelegateAfterNRequestsHandlerSettings
			{
				N = n,
				PreDelegate = async (request, ct) =>
				{
					preDelegateExecutedCount++;
				},
				PostDelegate = async (request, response, ct) =>
				{
					postDelegateExecutedCount++;
				},
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), delegateAfterNRequestsHandler);

		for (int i = 0; i < n; i++)
		{
			using var _ = await invoker.SendAsync(fixture.Create<HttpRequestMessage>(), CancellationToken.None);
		}
		using var _1 = await invoker.SendAsync(fixture.Create<HttpRequestMessage>(), CancellationToken.None);
		using var _2 = await invoker.SendAsync(fixture.Create<HttpRequestMessage>(), CancellationToken.None);

		Assert.AreEqual(2, preDelegateExecutedCount);
		Assert.AreEqual(2, postDelegateExecutedCount);
	}

	[Test]
	public async Task Does_Not_Run_PreDelegate_PostDelegate()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var n = 5;
		var preDelegateExecutedCount = 0;
		var postDelegateExecutedCount = 0;
		var delegateAfterNRequestsHandler = new DelegateAfterNRequestsHandler(
			new DelegateAfterNRequestsHandlerSettings
			{
				N = n,
				PreDelegate = async (request, ct) =>
				{
					preDelegateExecutedCount++;
				},
				PostDelegate = async (request, response, ct) =>
				{
					postDelegateExecutedCount++;
				},
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), delegateAfterNRequestsHandler);

		for (int i = 0; i < n; i++)
		{
			using var _ = await invoker.SendAsync(fixture.Create<HttpRequestMessage>(), CancellationToken.None);
		}
		Assert.AreEqual(0, preDelegateExecutedCount);
		Assert.AreEqual(0, postDelegateExecutedCount);
	}
}
