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

		public static void Register(object obj)
		{
			if (! initialized)
			{
				realContainer = FindRealContainer();
				initialized = true;
			}
			if (realContainer == null)
				return;
			try
			{
				realContainer.GetType().GetMethod("Register").Invoke(realContainer, new Object[] {obj});
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
	}
}

