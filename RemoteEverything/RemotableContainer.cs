using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteEverything
{
	public class RemotableContainer
	{

		public static RemotableContainer Instance
		{
			get
			{
				if (_instance == null)
					_instance = new RemotableContainer();
				return _instance;
			}
		}

		private static RemotableContainer _instance;

		private int nextId = 0;
		private readonly Dictionary<int, WeakReference> remotableInstances = new Dictionary<int, WeakReference>();

		public void Register(object remotable)
		{
			lock (remotableInstances)
			{
#if DEBUG
				Debug.Log(string.Format("Registered remotable instance: {0}", remotable));
#endif
				remotableInstances[nextId++] = new WeakReference(remotable);
			}
		}

		public void Walk(Action<int, object> callback)
		{
			lock(remotableInstances)
			{
				foreach (var obsolete in remotableInstances.Where(kv => ! kv.Value.IsAlive).ToList())
					remotableInstances.Remove(obsolete.Key);
				foreach (var reference in remotableInstances)
				{
					var obj = reference.Value.Target;
					if (obj != null)
						callback(reference.Key, obj);
				}
			}
		}
	}
}

