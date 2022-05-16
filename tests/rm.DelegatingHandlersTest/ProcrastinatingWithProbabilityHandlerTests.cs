using System.Diagnostics;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class ProcrastinatingWithProbabilityHandlerTests
	{
		[Test]
		public async Task Procrastinates()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var delayInMilliseconds = 25;
			var procrastinatingWithProbabilityHandler = new ProcrastinatingWithProbabilityHandler(
				new ProcrastinatingWithProbabilityHandlerSettings
				{
					ProbabilityPercentage = 100d,
					DelayInMilliseconds = delayInMilliseconds,
				});
			procrastinatingWithProbabilityHandler.InnerHandler = fixture.Create<HttpMessageHandler>();

			using var invoker = new HttpMessageInvoker(procrastinatingWithProbabilityHandler);

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

			var delayInMilliseconds = 1000;
			var procrastinatingWithProbabilityHandler = new ProcrastinatingWithProbabilityHandler(
				new ProcrastinatingWithProbabilityHandlerSettings
				{
					ProbabilityPercentage = 0d,
					DelayInMilliseconds = delayInMilliseconds,
				});
			procrastinatingWithProbabilityHandler.InnerHandler = fixture.Create<HttpMessageHandler>();

			using var invoker = new HttpMessageInvoker(procrastinatingWithProbabilityHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			var stopwatch = Stopwatch.StartNew();
			using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);
			stopwatch.Stop();
			Console.WriteLine(stopwatch.ElapsedMilliseconds);

			Assert.Less(stopwatch.ElapsedMilliseconds, delayInMilliseconds);
		}
	}
}
