using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RemoteEverything
{
	public static class ListRequest
	{
		class PerLogicalIdParams
		{
			readonly Dictionary<string, PerObjectParams> _perLogicalId;
			public PerLogicalIdParams(HttpListenerRequest request)
			{
				_perLogicalId = new Dictionary<string, PerObjectParams>();
				if (! request.HasEntityBody)
					return;

				var current = new StringBuilder();
				while (true)
				{
					var c = request.InputStream.ReadByte();
					if (c == -1 || c == '&') // store the last value
					{
						var components = current.ToString().Split('=');
						components[0] = Uri.UnescapeDataString(components[0]);
						PerObjectParams values;
						if (! _perLogicalId.TryGetValue(components[0], out values))
						{
							values = new PerObjectParams();
							_perLogicalId.Add(components[0], values);
						}
						values.Add(components[1]);
						if (c == -1)
							return;
						current = new StringBuilder();
					}
					else
					{
						current.Append((char)c);
					}
				}
			}

			public PerObjectParams TryGet(string logicalId)
			{
				PerObjectParams res = null;
				_perLogicalId.TryGetValue(logicalId, out res);
				return res;
			}
		}

		class PerObjectParams
		{
			readonly Dictionary<string, HashSet<string>> _perObjectParams = new Dictionary<string, HashSet<string>>();
			public void Add(string entry)
			{
				var components = Uri.UnescapeDataString(entry).Split(';');
				HashSet<string> members;
				if (!_perObjectParams.TryGetValue(components[0], out members))
				{
					members = new HashSet<string>();
					_perObjectParams.Add(components[0], members);
				}
				members.Add(components[1]);
			}

			public HashSet<string> TryGet(string objectName)
			{
				HashSet<string> res;
				_perObjectParams.TryGetValue(objectName, out res);
				return res;
			}
		}

		public static void HandleListRequest(System.IO.TextWriter stream, HttpListenerRequest request)
		{
			#if DEBUG
			Debug.Log("Processing list request");
			#endif

			PerLogicalIdParams param = null;
			try
			{
				param = new PerLogicalIdParams(request);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				throw new HttpServer.HttpException(400);
			}

			var objects = new Dictionary<string, Json.Node>();
			RemotableContainer.Instance.Walk(
				(obj, logicalId) =>	GetOrCreate(objects, logicalId).Add(obj.GetType().FullName, BuildObjectDescription(obj, param.TryGet(logicalId))));
			var result = new Json.Object();
			result.Add("objects", new Json.Object(objects));
			result.Write(stream);
		}

		static Json.Object GetOrCreate(Dictionary<string, Json.Node> container, string key)
		{
			Json.Node res;
			if (!container.TryGetValue(key, out res))
			{
				res = new Json.Object();
				container[key] = res;
			}
			return res as Json.Object;
		}

		static Json.Node BuildObjectDescription(object obj, PerObjectParams param)
		{
			var type = obj.GetType();
			var description = new Json.Object();
			HashSet<string> allowedFields = param?.TryGet(type.FullName);

			foreach (var kv in RemotableContent.Get(type).Exported)
				description.Add(
					kv.Key,
					BuildMember(
						kv.Value,
						obj,
						allowedFields?.Contains(kv.Key) ?? false));

			return description;
		}

		static Json.Node BuildMember(RemotableDetails details, object obj, bool fieldAllowed)
		{
			var info = details.Info;
			var content = new Json.Object();
			if (info is FieldInfo)
			{
				content.Add("type", Json.Node.MakeValue("field"));
				fieldAllowed = true;
			}
			else if (info is PropertyInfo)
				content.Add("type", Json.Node.MakeValue("property"));
			else if (info is MethodInfo)
			{
				content.Add("type", Json.Node.MakeValue("method"));
				content.Add(
					"parameters",
					new Json.List(
						details.parameters.Select(
							p => Json.Node.MakeValue(p.GetType().FullName)).ToList()
					)
				);
			}
			else
			{
				content.Add("type", Json.Node.MakeValue("unknown"));
				fieldAllowed = false;
			}

			if (details.valueType != null)
				content.Add("valueType", Json.Node.MakeValue(details.valueType.FullName));

			if (fieldAllowed)
			{
				if (details.AsDouble != null)
					content.Add("value", Json.Node.MakeValue(details.AsDouble(obj)));
				else if (details.AsString != null)
					content.Add("value", Json.Node.MakeValue(details.AsString(obj)));
			}

			if (details.DisplayName != null)
			{
				content.Add("displayName", Json.Node.MakeValue(details.DisplayName));
			}

			return content;
		}
	}
}

