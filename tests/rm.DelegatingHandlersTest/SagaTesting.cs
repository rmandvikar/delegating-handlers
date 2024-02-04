using AutoFixture;
using AutoFixture.AutoMoq;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;

namespace NsbTesting;

public class OrderSagaHander :
	NServiceBus.Saga<OrderSagaData>,
	IAmStartedByMessages<StartOrder>,
	IHandleMessages<CompleteOrder>
{
	protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
	{
		mapper.MapSaga(saga => saga.OrderId)
			.ToMessage<StartOrder>(message => message.OrderId)
			.ToMessage<CompleteOrder>(message => message.OrderId)
			;
	}

	public async Task Handle(StartOrder message, IMessageHandlerContext context)
	{
		Data.DramaProperty = "StartOrder";

		// work

		//return Task.CompletedTask;
		await context.Send(
			new CompleteOrder()
			{
				OrderId = message.OrderId
			});
	}

	public Task Handle(CompleteOrder message, IMessageHandlerContext context)
	{
		Data.DramaProperty = "CompleteOrder";

		// work

		//MarkAsComplete();
		return Task.CompletedTask;
	}
}

public class OrderSagaData :
	ContainSagaData
{
	public string OrderId { get; set; }
	public string DramaProperty { get; set; }
}

public class StartOrder : ICommand
{
	public string OrderId { get; set; }
}
public class CompleteOrder : ICommand
{
	public string OrderId { get; set; }
}

public class TransactionSagaHander :
	NServiceBus.Saga<OrderSagaData>,
	IAmStartedByMessages<StartOrder>,
	IHandleMessages<Step1Order>,
	IHandleMessages<Step2Order>,
	IHandleMessages<CompleteOrder>
{
	protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
	{
		mapper.MapSaga(saga => saga.OrderId)
			.ToMessage<StartOrder>(message => message.OrderId)
			.ToMessage<Step1Order>(message => message.OrderId)
			.ToMessage<Step2Order>(message => message.OrderId)
			.ToMessage<CompleteOrder>(message => message.OrderId)
			;
	}

	public async Task Handle(StartOrder message, IMessageHandlerContext context)
	{
		Data.DramaProperty = "StartOrder";

		// work

		//return Task.CompletedTask;
		await context.Send(
			new Step1Order()
			{
				OrderId = message.OrderId
			});
	}

	public async Task Handle(Step1Order message, IMessageHandlerContext context)
	{
		Data.DramaProperty = "Step1Order";

		// work

		//return Task.CompletedTask;
		await context.Send(
			new Step2Order()
			{
				OrderId = message.OrderId
			});
	}

	public async Task Handle(Step2Order message, IMessageHandlerContext context)
	{
		Data.DramaProperty = "Step2Order";

		// work

		//return Task.CompletedTask;
		await context.Send(
			new CompleteOrder()
			{
				OrderId = message.OrderId
			});
	}

	public Task Handle(CompleteOrder message, IMessageHandlerContext context)
	{
		Data.DramaProperty = "CompleteOrder";

		// work

		//MarkAsComplete();
		return Task.CompletedTask;
	}
}

public class Step1Order : ICommand
{
	public string OrderId { get; set; }
}
public class Step2Order : ICommand
{
	public string OrderId { get; set; }
}

public class OrderSagaTests
{
	[Test]
	public async Task __Verify_SagaData()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		var orderSagaData = fixture.Build<OrderSagaData>()
			.With(x => x.OrderId, "orderId")
			.With(x => x.DramaProperty, "NONE")
			.Create();
		var handler = new OrderSagaHander
		{
			Data = orderSagaData
		};

		var startOrder = fixture.Build<StartOrder>().With(x => x.OrderId, "orderId").Create();
		var context = new TestableMessageHandlerContext();

		await handler.Handle(startOrder, context);

		Assert.AreEqual("orderId", orderSagaData.OrderId);
		Assert.AreEqual("StartOrder", orderSagaData.DramaProperty);
	}

	[Test]
	public async Task __Verify_SagaData_Using_Testing()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		var orderSagaData = fixture.Build<OrderSagaData>()
			.With(x => x.OrderId, "orderId")
			.With(x => x.DramaProperty, "NONE")
			.Create();
		var handler = fixture.Build<OrderSagaHander>().With(saga => saga.Data, orderSagaData).Create();

		var testableSaga = new TestableSaga<OrderSagaHander, OrderSagaData>(sagaFactory: () => handler);

		var startOrder = fixture.Build<StartOrder>().With(x => x.OrderId, "orderId").Create();
		var context = new TestableMessageHandlerContext();

		var startOrderResult = await testableSaga.Handle(startOrder, context);

		var orderSagaDataSnapshot = startOrderResult.SagaDataSnapshot;

		Assert.AreEqual("orderId", orderSagaData.OrderId);
		Assert.AreEqual("NONE", orderSagaData.DramaProperty);

		Assert.AreEqual("orderId", orderSagaDataSnapshot.OrderId);
		Assert.AreEqual("StartOrder", orderSagaDataSnapshot.DramaProperty);

		var completeOrder = startOrderResult.FindSentMessage<CompleteOrder>();
		Assert.IsNotNull(completeOrder);
		Assert.AreEqual("orderId", completeOrder.OrderId);

		var completeOrderResult = await testableSaga.Handle(completeOrder, context);

		orderSagaDataSnapshot = completeOrderResult.SagaDataSnapshot;

		Assert.AreEqual("orderId", orderSagaDataSnapshot.OrderId);
		Assert.AreEqual("CompleteOrder", orderSagaDataSnapshot.DramaProperty);
	}
}

public class TransactionSagaTests
{
	[Test]
	public async Task __Verify_Transaction_SagaData()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		var orderSagaData = fixture.Build<OrderSagaData>()
			.With(x => x.OrderId, "orderId")
			.With(x => x.DramaProperty, "NONE")
			.Create();
		var handler = new TransactionSagaHander
		{
			Data = orderSagaData
		};

		var startOrder = fixture.Build<StartOrder>().With(x => x.OrderId, "orderId").Create();
		var context = new TestableMessageHandlerContext();

		await handler.Handle(startOrder, context);

		Assert.AreEqual("orderId", orderSagaData.OrderId);
		Assert.AreEqual("StartOrder", orderSagaData.DramaProperty);
	}

	[Test]
	public async Task __Verify_Transaction_SagaData_Using_Testing()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		var orderSagaData = fixture.Build<OrderSagaData>()
			.With(x => x.OrderId, "orderId")
			.With(x => x.DramaProperty, "NONE")
			.Create();
		var handler = fixture.Build<TransactionSagaHander>().With(saga => saga.Data, orderSagaData).Create();

		var testableSaga = new TestableSaga<TransactionSagaHander, OrderSagaData>(sagaFactory: () => handler);

		var startOrder = fixture.Build<StartOrder>().With(x => x.OrderId, "orderId").Create();
		var context = new TestableMessageHandlerContext();

		var startOrderResult = await testableSaga.Handle(startOrder, context);

		var orderSagaDataSnapshot = startOrderResult.SagaDataSnapshot;

		Assert.AreEqual("orderId", orderSagaData.OrderId);
		Assert.AreEqual("NONE", orderSagaData.DramaProperty);

		Assert.AreEqual("orderId", orderSagaDataSnapshot.OrderId);
		Assert.AreEqual("StartOrder", orderSagaDataSnapshot.DramaProperty);

		var step1Order = startOrderResult.FindSentMessage<Step1Order>();
		Assert.IsNotNull(step1Order);
		Assert.AreEqual("orderId", step1Order.OrderId);

		var completeOrderResult = await testableSaga.Handle(step1Order, context);

		orderSagaDataSnapshot = completeOrderResult.SagaDataSnapshot;

		Assert.AreEqual("orderId", orderSagaDataSnapshot.OrderId);
		Assert.AreEqual("Step1Order", orderSagaDataSnapshot.DramaProperty);
	}

	[Test]
	public async Task __Verify_Transaction_SagaData_Using_Testing_Previously()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());
		var orderSagaData = fixture.Build<OrderSagaData>()
			.With(x => x.OrderId, "orderId")
			.With(x => x.DramaProperty, "NONE")
			.Create();
		var handler = fixture.Build<TransactionSagaHander>().With(saga => saga.Data, orderSagaData).Create();

		var testableSaga = new TestableSaga<TransactionSagaHander, OrderSagaData>(sagaFactory: () => handler);

		var step2Order = fixture.Build<Step2Order>().With(x => x.OrderId, "orderId").Create();
		var context = new TestableMessageHandlerContext();

		// i should be able to run this test picking up from step2,
		//		step2order -> completeOrder
		// so i do not have to construct the whole scenario,
		//		startOrder -> step1Order -> step2order -> completeOrder
		//
		// but this test throws,
		//     System.Exception : Saga not found and message type NsbTesting.Step2Order is not allowed to start the saga.
		// this helps to slice the tests into small scenarios versus havin to repeat the scearnios leading up to step2
		var startOrderResult = await testableSaga.Handle(step2Order, context);

		var orderSagaDataSnapshot = startOrderResult.SagaDataSnapshot;

		Assert.AreEqual("orderId", orderSagaDataSnapshot.OrderId);
		Assert.AreEqual("Step2Order", orderSagaDataSnapshot.DramaProperty);

		var completeOrder = startOrderResult.FindSentMessage<CompleteOrder>();
		Assert.IsNotNull(completeOrder);
		Assert.AreEqual("orderId", completeOrder.OrderId);
	}
}
