﻿using System;
using NUnit.Framework;
using SevenDigital.Messaging.Integration.Tests._Helpers;
using SevenDigital.Messaging.Integration.Tests._Helpers.Handlers;
using SevenDigital.Messaging.Integration.Tests._Helpers.Messages;
using StructureMap;

namespace SevenDigital.Messaging.Integration.Tests.MessageSending
{
	[TestFixture]
	public class MessageSerialisationTests
	{
		IReceiver node_factory;

		protected TimeSpan LongInterval { get { return TimeSpan.FromMinutes(2); } }
		protected TimeSpan ShortInterval { get { return TimeSpan.FromSeconds(3); } }

		HoldingEventHook event_hook;
		private ISenderNode senderNode;

		[TestFixtureSetUp]
		public void StartMessaging()
		{
			Helper.SetupTestMessaging();
		}

		[SetUp]
		public void SetUp()
		{
			event_hook = new HoldingEventHook();

			ObjectFactory.Configure(map => map.For<IEventHook>().Use(event_hook));

			node_factory = MessagingSystem.Receiver();
			senderNode = MessagingSystem.Sender();
		}

		[Test]
		public void Sent_and_received_messages_should_be_equal()
		{
			using (node_factory.Listen(_=>_.Handle<IColourMessage>().With<ColourMessageHandler>()))
			{
				var message = new GreenMessage();

				senderNode.SendMessage(message);

				ColourMessageHandler.AutoResetEvent.WaitOne(LongInterval);
				HoldingEventHook.AutoResetEvent.WaitOne(ShortInterval);

				var sent = (IColourMessage)event_hook.sent;
				var received = (IColourMessage)event_hook.received;

				Assert.That(sent, Is.Not.Null, "sent message was null");
				Assert.That(received, Is.Not.Null, "received message was null");

				Assert.That(sent.CorrelationId, Is.EqualTo(received.CorrelationId));
				Assert.That(sent.Text, Is.EqualTo(received.Text));
			}
		}

		[TestFixtureTearDown]
		public void Stop() { MessagingSystem.Control.Shutdown(); }
	}
}
