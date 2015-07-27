using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RemoteEverything
{
	public class RemotableContent
	{
		public static RemotableContent get(Type t)
		{
			RemotableContent res;
			if (instances.TryGetValue(t, out res))
			{
				return res;
			}
			res = new RemotableContent(t);
			instances[t] = res;
			return res;
		}

		public Dictionary<string, MemberInfo> exported;

		static Dictionary<Type, RemotableContent> instances = new Dictionary<Type, RemotableContent>();

		RemotableContent(Type type)
		{
			exported = type.GetMembers(BindingFlags.GetField
				| BindingFlags.GetProperty
				| BindingFlags.Instance
				| BindingFlags.Public
				| BindingFlags.NonPublic
				| BindingFlags.FlattenHierarchy).Where(
					testCustomAttribute).ToDictionary(
						info => info.Name,
						info => info);
			#if DEBUG
			Debug.Log(string.Format("{0} attributes for {1}", exported.Count, type.Name));
			#endif
		}

		private static bool testCustomAttribute(MemberInfo info)
		{
			return info.GetCustomAttributes(false).Any(obj => obj.GetType().FullName.EndsWith(".Remotable"));
		}
	}
}

