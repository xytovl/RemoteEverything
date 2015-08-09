using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RemoteEverything
{
	public class RemotableDetails
	{
		public readonly MemberInfo Info;
		public readonly string DisplayName;
		public RemotableDetails(object attribute, MemberInfo info)
		{
			Info = info;
			DisplayName = GetFromAttribute<string>(attribute, "displayName");
		}
		static T GetFromAttribute<T>(object attribute, string fieldName) where T:class
		{
			var field = attribute.GetType().GetField(fieldName);
			if (field == null)
				return null;
			return field.GetValue(attribute) as T;
		}

	}

	public class RemotableContent
	{
		public static RemotableContent Get(Type t)
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

		public readonly Dictionary<string, RemotableDetails> Exported = new Dictionary<string, RemotableDetails>();

		static Dictionary<Type, RemotableContent> instances = new Dictionary<Type, RemotableContent>();

		RemotableContent(Type type)
		{
			foreach (var info in type.GetMembers(BindingFlags.GetField
				| BindingFlags.GetProperty
				| BindingFlags.Instance
				| BindingFlags.Public
				| BindingFlags.NonPublic
				| BindingFlags.FlattenHierarchy))
			{
				object attribute = FindRemotableAttribute(info);
				if (attribute != null)
				{
					Exported.Add(
							info.Name,
							new RemotableDetails(attribute, info));
				}
			}
			#if DEBUG
			Debug.Log(string.Format("{0} attributes for {1}", Exported.Count, type.Name));
			#endif
		}

		static object FindRemotableAttribute(MemberInfo info)
		{
			return info.GetCustomAttributes(false).FirstOrDefault(obj => obj.GetType().FullName.EndsWith(".Remotable", StringComparison.Ordinal));
		}
	}
}

