using System.Diagnostics;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class ProcrastinatingHandlerTests
	{
		[Test]
		public async Task Procrastinates()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var delayInMilliseconds = 25;
			var procrastinatingHandler = new ProcrastinatingHandler(
				new ProcrastinatingHandlerSettings
				{
					DelayInMilliseconds = delayInMilliseconds,
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				fixture.Create<HttpMessageHandler>(), procrastinatingHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			var stopwatch = Stopwatch.StartNew();
			using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);
			stopwatch.Stop();
			Console.WriteLine(stopwatch.ElapsedMilliseconds);

			Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, delayInMilliseconds);
		}
	}
}
