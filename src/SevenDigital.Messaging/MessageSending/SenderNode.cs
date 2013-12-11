using System;
using System.Collections.Generic;
using System.Threading;
using DispatchSharp;
using DispatchSharp.WorkerPools;
using SevenDigital.Messaging.Base;
using SevenDigital.Messaging.Base.Serialisation;
using SevenDigital.Messaging.Infrastructure;
using SevenDigital.Messaging.Logging;
using SevenDigital.Messaging.MessageReceiving;
// ReSharper disable RedundantUsingDirective
using DispatchSharp.QueueTypes;
// ReSharper restore RedundantUsingDirective

namespace SevenDigital.Messaging.MessageSending
{
	/// <summary>
	/// Standard sender node for Messaging.
	/// You do not need to create this yourself. Use `MessagingSystem.Sender()`
	/// </summary>
	public class SenderNode : ISenderNode
	{
		const int SingleThreaded = 1;
		readonly IMessagingBase _messagingBase;
		readonly ISleepWrapper _sleeper;
		readonly IOutgoingQueueFactory _queueFactory;
		IDispatch<byte[]> _sendingDispatcher;
		PersistentWorkQueue _persistentQueue;

		/// <summary>
		/// Create a new message sending node. You do not need to create this yourself.
		/// Use `MessagingSystem.Sender()`
		/// </summary>
		public SenderNode(
			IMessagingBase messagingBase,
			IDispatcherFactory dispatchFactory,
			ISleepWrapper sleeper,
			IOutgoingQueueFactory queueFactory
			)
		{
			_messagingBase = messagingBase;
			_sleeper = sleeper;
			_queueFactory = queueFactory;


			_persistentQueue = new PersistentWorkQueue(_queueFactory, _sleeper);

			_sendingDispatcher = dispatchFactory.Create(
				_persistentQueue,
				new ThreadedWorkerPool<byte[]>("SDMessaging_Sender")
			);

			_sendingDispatcher.SetMaximumInflight(SingleThreaded);
			_sendingDispatcher.AddConsumer(SendWaitingMessage);
			_sendingDispatcher.Exceptions += SendingExceptions;
			_sendingDispatcher.Start();
		}

		/// <summary>
		/// Handle exceptions thrown during sending.
		/// </summary>
		public void SendingExceptions(object sender, ExceptionEventArgs<byte[]> e)
		{
			_sleeper.SleepMore();
			e.WorkItem.Cancel();

			Log.Warning("Sender failed: " + e.SourceException.GetType() + "; " + e.SourceException.Message);
		}

		/// <summary>
		/// Send the given message. Does not guarantee reception.
		/// </summary>
		/// <param name="message">Message to be send. This must be a serialisable type</param>
		public virtual void SendMessage<T>(T message) where T : class, IMessage
		{
			var prepared = _messagingBase.PrepareForSend(message);
			_sendingDispatcher.AddWork(prepared.ToBytes());
			HookHelper.TrySentHooks(message);
		}

		/// <summary>
		/// Internal immediate send. Use SendMessage() instead.
		/// </summary>
		public void SendWaitingMessage(byte[] message)
		{
			_messagingBase.SendPrepared(PreparedMessage.FromBytes(message));
			_sleeper.Reset();
		}

		/// <summary>
		/// Shutdown the sender
		/// </summary>
		public void Dispose()
		{
			var lDispatcher = Interlocked.Exchange(ref _sendingDispatcher, null);
			if (lDispatcher != null)
				//lDispatcher.Stop();
				lDispatcher.WaitForEmptyQueueAndStop(MessagingSystem.ShutdownTimeout);

			var lQueue = Interlocked.Exchange(ref _persistentQueue, null);
			if (lQueue != null)
				lQueue.Dispose();

			_queueFactory.Cleanup();
		}
	}
}