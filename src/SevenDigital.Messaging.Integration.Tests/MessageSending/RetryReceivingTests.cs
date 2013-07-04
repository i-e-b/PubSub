using System;// ReSharper disable InconsistentNaming
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using NUnit.Framework;
using SevenDigital.Messaging.EventHooks;
using SevenDigital.Messaging.Integration.Tests.Messages;
using StructureMap;

namespace SevenDigital.Messaging.Integration.Tests
{
	[TestFixture]
	public class RetryReceivingTests
	{
		IReceiver _receiver;
		ISenderNode _senderNode;

		protected TimeSpan LongInterval
		{
			get { return TimeSpan.FromSeconds(20); }
		}

		protected TimeSpan ShortInterval
		{
			get { return TimeSpan.FromSeconds(3); }
		}

		[SetUp]
		public void SetUp()
		{
			Helper.SetupTestMessaging();
			MessagingSystem.Events.AddEventHook<ConsoleEventHook>();
			_receiver = MessagingSystem.Receiver();
			_senderNode = MessagingSystem.Sender();
		}

		[Test]
		public void Handler_should_retry_on_matched_exceptions_but_not_un_unmatched_exceptions()
		{
			ExceptionSample.handledTimes = 0;
			ExceptionSample.AutoResetEvent = new AutoResetEvent(false);
			using (_receiver.Listen(_=>_.Handle<IColourMessage>().With<ExceptionSample>()))
			{

				_senderNode.SendMessage(new RedMessage());

				ExceptionSample.AutoResetEvent.WaitOne(ShortInterval);
				Assert.That(ExceptionSample.handledTimes, Is.EqualTo(2));
			}
		}

		[Test]
		public void Should_send_handler_failed_event_after_retry ()
		{
			ExceptionSample.handledTimes = 0;
			ExceptionSample.AutoResetEvent = new AutoResetEvent(false);
			var events = new TestEvents();
			var hook = new TestEventHook(events);
			ObjectFactory.Configure(map => map.For<IEventHook>().Use(hook));
			using (_receiver.Listen(_=>_.Handle<IColourMessage>().With<ExceptionSample>()))
			{
				_senderNode.SendMessage(new RedMessage());

				ExceptionSample.AutoResetEvent.WaitOne(ShortInterval);
			}

			ObjectFactory.GetInstance<IEventHook>();
			Assert.That(events.HandlerExceptions.Count(e => e is IOException), Is.EqualTo(1));
		}



		[TestFixtureTearDown]
		public void Stop() { MessagingSystem.Control.Shutdown(); }


		[RetryMessage(typeof(IOException))]
		[RetryMessage(typeof(WebException))]
		public class ExceptionSample : IHandle<IColourMessage>
		{
			public static int handledTimes = 0;
			readonly object lockobj = new Object();

			public static AutoResetEvent AutoResetEvent { get; set; }

			public void Handle(IColourMessage message)
			{
				lock (lockobj)
				{
					handledTimes++;
				}

				if (handledTimes == 1)
				{
					throw new IOException();
				}
				AutoResetEvent.Set();
				throw new InvalidOperationException();
			}
		}
	}
}