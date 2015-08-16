using System;
using System.Reflection;
using UnityEngine;

namespace ChangeMe
{
	public class Remotable : System.Attribute
	{
		public string displayName;
	}

	public static class RemotableContainer
	{
		static bool initialized = false;
		static object realContainer;

		static object Container { get
			{
				if (! initialized)
				{
					realContainer = FindRealContainer();
					initialized = true;
				}
				return realContainer;
			}
		}

		static object FindRealContainer()
		{
			try 
			{
				foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
				{
					foreach (var t in a.GetExportedTypes())
					{
						if (t.FullName == "RemoteEverything.RemotableContainer")
							return t.GetProperty("Instance").GetValue(null, null);
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
			return null;
		}

		public static void Register(object obj, string logicalId)
		{
			var container = Container;
			if (container == null)
				return;
			try
			{
				container.GetType().GetMethod("Register").Invoke(realContainer, new object[] {obj, logicalId});
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		public static void ManualRegisterMember(object obj, string logicalId, MemberInfo member)
		{
			var container = Container;
			if (container == null)
				return;
			try
			{
				container.GetType().GetMethod("ManualRegisterMember").Invoke(realContainer, new object[] {obj, logicalId, member});
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		public static void Unregister(object obj)
		{
			var container = Container;
			if (container == null)
				return;
			try
			{
				container.GetType().GetMethod("Unregister").Invoke(realContainer, new object[] {obj});
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
	}
}

