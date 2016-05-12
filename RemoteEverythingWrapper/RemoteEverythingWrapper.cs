using System;
using System.Reflection;
using System.Collections.Generic;
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
					try
					{
						foreach (var t in a.GetExportedTypes())
						{
							if (t.FullName == "RemoteEverything.RemotableContainer")
								return t.GetProperty("Instance").GetValue(null, null);
						}
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
			return null;
		}

		public static void Register(object obj, string logicalId, string logicalIdName = null)
		{
			var container = Container;
			if (container == null)
				return;
			try
			{
				var moreInfo = new Dictionary<string, object>();
				if (logicalIdName != null)
					moreInfo["LogicalIdName"] = logicalIdName;
				container.GetType().GetMethod("Register").Invoke(realContainer, new object[] {obj, logicalId, moreInfo});
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		public static void ManualRegisterMember(Type obj, MemberInfo member, string displayName = null)
		{
			var container = Container;
			if (container == null)
				return;
			try
			{
				var moreInfo = new Dictionary<string, object>();
				if (displayName != null)
					moreInfo["DisplayName"] = displayName;

				container.GetType().GetMethod("ManualRegisterMember").Invoke(realContainer, new object[] {obj, member, moreInfo});
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

