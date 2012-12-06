using System;
using System.Collections.Generic;
using System.Linq;
using SevenDigital.Messaging.Base;

namespace SevenDigital.Messaging.Dispatch
{
	public class MessageDispatcher : IMessageDispatcher
	{
		readonly IThreadPoolWrapper threadPoolWrapper;
		readonly Dictionary<Type, ActionList> handlers;

		public MessageDispatcher(IThreadPoolWrapper threadPoolWrapper)
		{
			this.threadPoolWrapper = threadPoolWrapper;
			handlers = new Dictionary<Type, ActionList>();
		}

		public void TryDispatch(object messageObject)
		{
			var type = messageObject.GetType().DirectlyImplementedInterfaces().Single();
			var actions = handlers[type].GetClosed(messageObject);
			foreach (var action in actions)
			{
				threadPoolWrapper.Do(action);
			}
		}

		public void AddHandler<T>(Action<T> handlerAction)
		{
			lock (handlers)
			{
				if (!handlers.ContainsKey(typeof(T)))
				{
					handlers.Add(typeof(T), new ActionList());
				}
				handlers[typeof(T)].Add(handlerAction);
			}
		}

		public IEnumerable<Action<T>> HandlersForType<T>()
		{
			return handlers[typeof(T)].GetOfType<T>();
		}

		class ActionList
		{
			readonly List<object> list;

			public ActionList()
			{
				list = new List<object>();
			}

			public void Add<T>(Action<T> act)
			{
				list.Add(act);
			}

			public IEnumerable<Action<T>> GetOfType<T>()
			{
				return list.Select(boxed => (Action<T>)boxed);
			}

			public IEnumerable<Action> GetClosed(object obj)
			{
				return list.Select(boxed => unboxAndEnclose(boxed, obj));
			}

			Action unboxAndEnclose(object boxedAction, object obj)
			{
				var x = (Delegate)boxedAction;
				return () => x.DynamicInvoke(obj);
			}
		}
	}

}