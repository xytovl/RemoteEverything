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

		public readonly Func<object, double> AsDouble;
		public readonly Func<object, string> AsString;

		// Type of the field value, property value, method return value
		// null for set-only property
		public readonly Type valueType;

		// For methods, parameter count and type
		public readonly ParameterInfo[] parameters;

		public RemotableDetails(object attribute, MemberInfo info)
		{
			Info = info;
			if (attribute != null)
				DisplayName = GetFromAttribute<string>(attribute, "displayName");

			// Build the converter object
			Func<object, object> getValue = null;

			var fieldInfo = Info as FieldInfo;
			if (fieldInfo != null)
			{
				valueType = fieldInfo.FieldType;
				getValue = fieldInfo.GetValue;
			}

			var propertyInfo = Info as PropertyInfo;
			if (propertyInfo != null)
			{
				valueType = propertyInfo.PropertyType;
				if (propertyInfo.CanRead)
					getValue = instance => propertyInfo.GetValue(instance, null);
			}

			var methodInfo = Info as MethodInfo;
			if (methodInfo != null)
			{
				valueType = methodInfo.ReturnType;
				parameters = methodInfo.GetParameters();
				if (parameters.All(p => p.IsOptional || p.DefaultValue != DBNull.Value))
				{
					object[] args = null;
					if (parameters.Length != 0)
						args = parameters.Select(p => p.IsOptional ? Type.Missing : p.DefaultValue).ToArray();
					getValue = instance => methodInfo.Invoke(instance, args);
				}
			}

			if (valueType != null && getValue != null)
			{
				var converter = new TypeConverter(valueType);
				if (converter.AsDouble != null)
				{
					AsDouble = instance => converter.AsDouble(getValue(instance));
				}
				if (converter.AsString != null)
				{
					AsString = instance => converter.AsString(getValue(instance));
				}
			}

		}

		public RemotableDetails(Dictionary<string, object> moreInfo, MemberInfo info): this((object)null, info)
		{
			if (moreInfo.ContainsKey("DisplayName"))
				DisplayName = moreInfo["DisplayName"] as string;
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

		public void AddMember(MemberInfo info, Dictionary<string, object> moreInfo)
		{
			if (!Exported.ContainsKey(info.Name)) Exported.Add(info.Name, new RemotableDetails(moreInfo, info));
		}
	}
}

