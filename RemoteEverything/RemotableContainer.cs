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

		private struct Ref
		{
			public readonly WeakReference Reference;
			public readonly string LogicalId;
			public Ref(object obj, string logicalId)
			{
				Reference = new WeakReference(obj);
				LogicalId = logicalId;
			}
		}

		private static RemotableContainer _instance;

		private int nextId = 0;
		private readonly Dictionary<int, Ref> remotableInstances = new Dictionary<int, Ref>();

		public void Register(object remotable, string logicalId)
		{
			if (remotable == null)
				throw new ArgumentNullException("remotable", "null can not be registered as remotable");
			lock (remotableInstances)
			{
				if (remotableInstances.Any(kv => kv.Value.Reference.Target == remotable))
				{
#if DEBUG
					Debug.Log(string.Format("Object {0} already registered, ignoring", remotable));
#endif
					return;
				}

#if DEBUG
				Debug.Log(string.Format("Registered remotable instance: {0}", remotable));
#endif
				remotableInstances[nextId++] = new Ref(remotable, logicalId);
			}
		}

		public void UnRegister(object remotable)
		{
			lock (remotableInstances)
			{
				var obj = remotableInstances.FirstOrDefault(kv => kv.Value.Reference.Target == remotable);
				if (obj.Value.Reference != null)
					remotableInstances.Remove(obj.Key);
			}
		}

		public void Walk(Action<int, object, string> callback)
		{
			lock(remotableInstances)
			{
				foreach (var obsolete in remotableInstances.Where(kv => ! kv.Value.Reference.IsAlive).ToList())
					remotableInstances.Remove(obsolete.Key);
				foreach (var reference in remotableInstances)
				{
					var obj = reference.Value.Reference.Target;
					if (obj != null)
						callback(reference.Key, obj, reference.Value.LogicalId);
				}
			}
		}
	}
}

