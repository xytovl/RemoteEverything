using System;
using System.Reflection;

namespace ChangeMe
{
	public class Remotable : System.Attribute
	{
		public string name;
	}

	public static class RemotableContainer
	{
		static bool initialized = false;
		static object realContainer;

		static object findRealContainer()
		{
			try 
			{
				//var t = Type.GetType("");
				foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
				{
					foreach (var t in a.GetExportedTypes())
					{
						if (t.FullName == "RemoteEverything.RemotableContainer")
							return t.GetProperty("instance").GetValue(null, null);
					}
				}
			}
			catch (Exception e)
			{
			}
			return null;
		}

		public static void register(object obj)
		{
			if (! initialized)
			{
				realContainer = findRealContainer();
				initialized = true;
			}
			if (realContainer == null)
				return;
			try
			{
				realContainer.GetType().GetMethod("register").Invoke(realContainer, new Object[] {obj});
			}
			catch (Exception e)
			{
			}
		}
	}
}

