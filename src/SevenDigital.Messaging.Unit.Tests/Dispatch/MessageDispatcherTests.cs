﻿using System;
using NUnit.Framework;
using SevenDigital.Messaging.Base;
using SevenDigital.Messaging.Dispatch;
using SevenDigital.Messaging.MessageSending;

namespace SevenDigital.Messaging.Unit.Tests.Dispatch
{
	[TestFixture]
	public class MessageDispatcherTests
	{
		IMessageDispatcher subject;
		HandlerAction<ITestMessage> testHandler;
		HandlerAction<ITestMessage> anotherHandler;
		HandlerAction<IDifferentTypeMessage> aDifferentType;
		IWorkWrapper mockWork;

		volatile int testHandlerHits;
		volatile int anotherHandlerHits;
		volatile int aDifferentHits;

		[SetUp]
		public void A_message_dispatcher ()
		{
			mockWork = new FakeWork();
			subject = new MessageDispatcher(mockWork);
			testHandler = msg => { testHandlerHits++; return null;};
			anotherHandler = msg => { anotherHandlerHits++; return null;};
			aDifferentType = msg => { aDifferentHits++; return null;};
		}

		[Test]
		public void When_adding_handler_should_have_that_handler_registered ()
		{
			subject.AddHandler(testHandler);
			Assert.That(((MessageDispatcher)subject).HandlersForType<ITestMessage>(), 
				Is.EquivalentTo( new [] {testHandler} ));
		}

		[Test]
		public void When_adding_more_than_one_handler_of_a_given_type_should_have_all_registered ()
		{
			subject.AddHandler(testHandler);
			subject.AddHandler(anotherHandler);
			Assert.That(((MessageDispatcher)subject).HandlersForType<ITestMessage>(), 
				Is.EquivalentTo( new [] {testHandler, anotherHandler} ));
		}

		[Test]
		public void When_adding_different_types_of_handler_they_should_not_be_registered_together ()
		{
			subject.AddHandler(testHandler);
			subject.AddHandler(anotherHandler);
			subject.AddHandler(aDifferentType);

			Assert.That(((MessageDispatcher)subject).HandlersForType<ITestMessage>(), 
				Is.EquivalentTo( new [] {testHandler, anotherHandler} ));

			Assert.That(((MessageDispatcher)subject).HandlersForType<IDifferentTypeMessage>(), 
				Is.EquivalentTo( new [] {aDifferentType} ));
		}

		[Test]
		public void When_dispatching_a_message_should_send_all_matching_handlers_to_thread_pool ()
		{
			testHandlerHits = anotherHandlerHits = aDifferentHits = 0;
			subject.AddHandler(testHandler);
			subject.AddHandler(anotherHandler);
			subject.AddHandler(aDifferentType);

			subject.TryDispatch(Wrap(new FakeMessage()));

			Assert.That(testHandlerHits, Is.EqualTo(1));
			Assert.That(anotherHandlerHits, Is.EqualTo(1));
			Assert.That(aDifferentHits, Is.EqualTo(0));
		}
		
		[Test]
		public void When_dispatching_a_super_class_message_should_send_all_matching_handlers_to_thread_pool ()
		{
			testHandlerHits = anotherHandlerHits = aDifferentHits = 0;
			subject.AddHandler(testHandler);
			subject.AddHandler(anotherHandler);
			subject.AddHandler(aDifferentType);

			subject.TryDispatch(Wrap(new SuperMessage()));

			Assert.That(testHandlerHits, Is.EqualTo(1));
			Assert.That(anotherHandlerHits, Is.EqualTo(1));
			Assert.That(aDifferentHits, Is.EqualTo(0));
		}

        IPendingMessage<T> Wrap<T>(T message)
        {
            return new PendingMessage<T> {
                Message = message,
                Cancel = () => { },
                Finish = () => { }
            };
        }

	}

	public class FakeWork : IWorkWrapper
	{
		public void Do(Action action)
		{
			action();
		}
	}

	public class FakeMessage:ITestMessage
	{
		public Guid CorrelationId { get; set; }
	}
	public class SuperMessage:ISuperTestMessage
	{
		public Guid CorrelationId { get; set; }
	}
	
	interface ITestMessage:IMessage
	{
	}
	interface ISuperTestMessage:ITestMessage
	{
	}
	interface IDifferentTypeMessage:IMessage
	{
	}
}
