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

		private readonly Dictionary<string, Dictionary<Type, WeakReference>> remotableInstances = new Dictionary<string, Dictionary<Type, WeakReference>>();

		public void Register(object remotable, string logicalId)
		{
			if (remotable == null)
				throw new ArgumentNullException("remotable", "null can not be registered as remotable");

			var type = remotable.GetType();
			lock (remotableInstances)
			{
				Dictionary<Type, WeakReference> registeredTypes;
				if (remotableInstances.TryGetValue(logicalId, out registeredTypes))
				{
					WeakReference target;
					if (registeredTypes.TryGetValue(type, out target))
					{
						if (target.Target == remotable)
						{
#if DEBUG
							Debug.Log(string.Format("Object {0} already registered, ignoring", remotable));
#endif
							return;
						}
						throw new Exception(string.Format("Tried to register twice an object of type {0} with logical id {1}", type, logicalId));
					}
				}
				else
				{
					registeredTypes = new Dictionary<Type, WeakReference>();
					remotableInstances[logicalId] = registeredTypes;
				}
				registeredTypes[type] = new WeakReference(remotable);
#if DEBUG
				Debug.Log(string.Format("Registered remotable instance: {0}", remotable));
#endif
			}
		}

		public void Unregister(object remotable)
		{
			lock (remotableInstances)
			{
				foreach (var registeredTypes in remotableInstances.Values)
				{
					WeakReference target;
					if (registeredTypes.TryGetValue(remotable.GetType(), out target) && target.Target == remotable)
						registeredTypes.Remove(remotable.GetType());
				}
				cleanup();
			}
		}

		public void Walk(Action<object, string> callback)
		{
			lock(remotableInstances)
			{
				cleanup();
				foreach (var perLogicalId in remotableInstances)
				{
					foreach (var reference in perLogicalId.Value)
					{
						var obj = reference.Value.Target;
						if (obj != null)
							callback(obj, perLogicalId.Key);
					}
				}
			}
		}

		void cleanup()
		{
			lock(remotableInstances)
			{
				var emptyLogicalIds = new List<string>();
				foreach (var perLogicalId in remotableInstances)
				{
					foreach (var obsolete in perLogicalId.Value.Where(kv => ! kv.Value.IsAlive).ToList())
						perLogicalId.Value.Remove(obsolete.Key);
					if (perLogicalId.Value.Count == 0)
						emptyLogicalIds.Add(perLogicalId.Key);
				}
			foreach (var id in emptyLogicalIds)
				remotableInstances.Remove(id);
			}
		}
	}
}

