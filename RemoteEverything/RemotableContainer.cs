using System;
using System.Collections.Generic;
using System.Linq;

namespace RemoteEverything
{
	public class RemotableContainer
	{

		public static RemotableContainer instance
		{
			get
			{
				if (_instance == null)
					_instance = new RemotableContainer();
				return _instance;
			}
		}

		private static RemotableContainer _instance;

		private List<WeakReference> remotableInstances = new List<WeakReference>();

		private RemotableContainer ()
		{
		}

		public void register(object remotable)
		{
			remotableInstances.Add(new WeakReference(remotable));
		}

		public void walk(Action<object> callback)
		{
			remotableInstances.RemoveAll(reference => !reference.IsAlive);
			foreach (var reference in remotableInstances)
			{
				var obj = reference.Target;
				if (obj != null)
					callback(obj);
			}
		}
	}
}

