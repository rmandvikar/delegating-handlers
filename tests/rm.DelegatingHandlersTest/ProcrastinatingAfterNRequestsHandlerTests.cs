using System.Diagnostics;
using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ProcrastinatingAfterNRequestsHandlerTests
{
	[Retry(5)]
	[Test]
	public async Task Procrastinates()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var n = 5;
		var delayInMilliseconds = 25;
		var procrastinatingAfterNRequestsHandler = new ProcrastinatingAfterNRequestsHandler(
			new ProcrastinatingAfterNRequestsHandlerSettings
			{
				N = n,
				DelayInMilliseconds = delayInMilliseconds,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), procrastinatingAfterNRequestsHandler);

		for (int i = 0; i < n; i++)
		{
			using var _ = await invoker.SendAsync(fixture.Create<HttpRequestMessage>(), CancellationToken.None);
		}
		using var requestMessage = fixture.Create<HttpRequestMessage>();
		var stopwatch = Stopwatch.StartNew();
		using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);
		stopwatch.Stop();
		Console.WriteLine(stopwatch.ElapsedMilliseconds);

		Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, delayInMilliseconds);
	}

	[Test]
	public async Task Does_Not_Procrastinate()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var n = 5;
		var delayInMilliseconds = 1000;
		var procrastinatingAfterNRequestsHandler = new ProcrastinatingAfterNRequestsHandler(
			new ProcrastinatingAfterNRequestsHandlerSettings
			{
				N = n,
				DelayInMilliseconds = delayInMilliseconds,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			fixture.Create<HttpMessageHandler>(), procrastinatingAfterNRequestsHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		var stopwatch = Stopwatch.StartNew();
		using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);
		stopwatch.Stop();
		Console.WriteLine(stopwatch.ElapsedMilliseconds);

		Assert.Less(stopwatch.ElapsedMilliseconds, delayInMilliseconds);
	}
}
