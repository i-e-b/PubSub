using System;
using System.Linq;
using SevenDigital.Messaging.Base.RabbitMq;
using SevenDigital.Messaging.Logging;
using SevenDigital.Messaging.MessageReceiving;
using SevenDigital.Messaging.Routing;
using StructureMap;

namespace SevenDigital.Messaging.ConfigurationActions
{
	class SDM_Control : IMessagingControl
	{
		public void Shutdown()
		{
			lock (MessagingSystem.ConfigurationLock)
			{
				Log.Instance().Shutdown();

				EjectAndDispose<IReceiverControl>();
				EjectAndDispose<IReceiver>();

				EjectAndDispose<IEventHook>();

				EjectAndDispose<ISenderNode>();

				EjectAndDispose<IUniqueEndpointGenerator>();
				EjectAndDispose<ISleepWrapper>();

				EjectAndDispose<IMessagingHost>();
				EjectAndDispose<IRabbitMqConnection>();
				EjectAndDispose<IChannelAction>();
			}
		}

		public void OnInternalWarning(Action<MessagingLogEventArgs> action)
		{
			Log.Instance().RegisterAction(action);
		}

		public static void EjectAndDispose<T>()
		{
			IDisposable[] instances;

			try
			{
				instances = ObjectFactory.GetAllInstances<T>().OfType<IDisposable>().ToArray();
			}
			catch
			{
				instances = new IDisposable[0];
			}

			ObjectFactory.EjectAllInstancesOf<T>();
			if (instances.Length < 1) return;

			foreach (var disposable in instances) disposable.Dispose();
		}

		public void SetConcurrentHandlers(int max)
		{
			if (MessagingSystem.UsingLoopbackMode()) return;

			if (max < 1) throw new ArgumentException("Concurrent handlers must be at least 1", "max");

			var controller = ObjectFactory.TryGetInstance<IReceiver>() as IReceiverControl;
			if (controller == null) throw new InvalidOperationException("Messaging is not configured");

			controller.SetConcurrentHandlers(max);
			MessagingSystem.Concurrency = max;
		}

		public void Pause()
		{
			var controller = ObjectFactory.TryGetInstance<IReceiver>() as IReceiverControl;
			if (controller == null) throw new InvalidOperationException("Messaging is not configured");
			controller.SetConcurrentHandlers(0);
		}
	}
}