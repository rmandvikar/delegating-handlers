using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class HttpMessageInvokerFactoryTests
{
	[Test]
	public async Task Verify_Create_DelegatingHandler()
	{
		using var invoker = HttpMessageInvokerFactory.Create(new RelayHandler());
	}

	[Test]
	public async Task Verify_Create_DelegatingHandlers()
	{
		using var invoker = HttpMessageInvokerFactory.Create(new RelayHandler(), new RelayHandler());
	}

	[Test]
	public async Task Verify_Create_HttpMessageHandler()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		using var invoker = HttpMessageInvokerFactory.Create(fixture.Create<HttpMessageHandler>());
	}

	[Test]
	public async Task Verify_Create_HttpMessageHandler_DelegatingHandler()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		using var invoker = HttpMessageInvokerFactory.Create(fixture.Create<HttpMessageHandler>(), new RelayHandler());
	}

	[Test]
	public async Task Verify_Create_HttpMessageHandler_DelegatingHandlers()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		using var invoker = HttpMessageInvokerFactory.Create(fixture.Create<HttpMessageHandler>(), new RelayHandler(), new RelayHandler());
	}

	[Test]
	public async Task Verify_Create_None()
	{
		Assert.Throws<ArgumentNullException>(() =>
		{
			using var invoker = HttpMessageInvokerFactory.Create();
		});
	}

	[Test]
	public async Task Verify_Create_None_Null()
	{
		Assert.Throws<ArgumentNullException>(() =>
		{
			using var invoker = HttpMessageInvokerFactory.Create(handlers: null!);
		});
	}

	[Test]
	public async Task Verify_Create_Null_Null()
	{
		Assert.Throws<ArgumentNullException>(() =>
		{
			using var invoker = HttpMessageInvokerFactory.Create(innerHandler: null!, handlers: null!);
		});
	}

	[Test]
	public async Task Verify_Create_Value_Null()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		using var invoker = HttpMessageInvokerFactory.Create(fixture.Create<HttpMessageHandler>(), handlers: null!);
	}

	[Test]
	public async Task Verify_Create_DelegatingHandler_Null()
	{
		Assert.Throws<ArgumentNullException>(() =>
		{
			using var invoker = HttpMessageInvokerFactory.Create(handlers: [null!]);
		});
	}
}
