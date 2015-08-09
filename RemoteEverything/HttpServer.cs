using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEngine;

namespace RemoteEverything
{
	public class HttpServer
	{

		class HttpException: Exception
		{
			public int _code;
			public HttpException(int code)
			{
				_code = code;
			}
		}

		HttpListener listener;
		List<HttpListenerContext> pending = new List<HttpListenerContext>();

		public HttpServer(ushort port)
		{
			listener = new HttpListener();
			listener.Prefixes.Add(string.Format("http://*:{0}/", port));
			listener.Start();
			listener.BeginGetContext(ChainRequests, null);
		}

		public void Terminate()
		{
			lock(listener)
			{
				foreach (var request in pending)
				{
					try
					{
						request.Response.Abort();
					}
					catch (Exception e)
					{
					Debug.LogException(e);
					}
				}
			}
			listener.Close();
		}

		// To be called in main thread
		public void ProcessRequests()
		{
			List<HttpListenerContext> requests;
			lock(listener)
			{
				if (pending.Count == 0)
					return;
				requests = pending;
				pending = new List<HttpListenerContext>();
			}
			foreach (var request in requests)
			{
				try
				{
					HandleRequest(request);
				}
				catch (Exception e)
				{
					var httpError = e as HttpException;
					if (httpError != null)
						request.Response.StatusCode = httpError._code;
					Debug.LogException(e);
				}
				request.Response.Close();
			}
		}

		void ChainRequests(IAsyncResult result)
		{
			try
			{
				var ctx = listener.EndGetContext(result);
				lock(listener)
				{
					pending.Add(ctx);
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
			if (listener.IsListening)
				listener.BeginGetContext(ChainRequests, null);
		}

		void HandleRequest(HttpListenerContext ctx)
		{
			#if DEBUG
			Debug.Log("Handling request " + ctx.Request.RawUrl);
			#endif

			var uri = ctx.Request.Url;

			if (uri == null)
				throw new HttpException(400);

			#if DEBUG
			Debug.Log("Path: " + uri.AbsolutePath);
			#endif

			var memStream = new System.IO.MemoryStream();
			var outputStream = new System.IO.StreamWriter(memStream);

			switch(uri.AbsolutePath)
			{
			case "/list":
				HandleListRequest(outputStream);
				break;
			default:
				throw new HttpException(404);
			}
			outputStream.Flush();
			ctx.Response.Headers.Add("Access-Control-Allow-Origin: *");
			ctx.Response.ContentLength64 = memStream.Length;
			memStream.WriteTo(ctx.Response.OutputStream);
		}

		void HandleListRequest(System.IO.TextWriter stream)
		{
			#if DEBUG
			Debug.Log("Processing list request");
			#endif
			var objects = new Dictionary<string, Json.Node>();
			RemotableContainer.Instance.Walk(
					(obj, logicalId) =>	GetOrCreate(objects, logicalId).Add(obj.GetType().FullName, BuildObjectDescription(obj)));
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

		static Json.Node BuildObjectDescription(object obj)
		{
			var type = obj.GetType();
			var description = new Json.Object();

			foreach (var kv in RemotableContent.Get(type).Exported)
				description.Add(kv.Key, BuildMember(kv.Value, obj));

			return description;
		}

		static Json.Node BuildMember(RemotableDetails details, object obj)
		{
			var info = details.Info;
			var content = new Json.Object();
			if (info is FieldInfo)
				content.Add("type", Json.Node.MakeValue("field"));
			else if (info is PropertyInfo)
				content.Add("type", Json.Node.MakeValue("property"));
			else if (info is MethodInfo)
				content.Add("type", Json.Node.MakeValue("method"));
			else
				content.Add("type", Json.Node.MakeValue("unknown"));

			var value = BuildValue(info, obj);
			if (value != null)
				content.Add("value", value);

			if (details.DisplayName != null)
			{
				content.Add("displayName", Json.Node.MakeValue(details.DisplayName));
			}

			return content;
		}

		static Json.Node BuildValue(MemberInfo info, object obj)
		{
			var fieldInfo = info as FieldInfo;
			if (fieldInfo != null)
			{
				var val = fieldInfo.GetValue(obj);
				if (fieldInfo.FieldType == typeof(string))
					return Json.Node.MakeValue(val as string);
				if (fieldInfo.FieldType == typeof(double))
					return Json.Node.MakeValue((double)val);
			}
			Debug.Log(string.Format("failed to print value of {0}", info));
			return null;
		}
	}
}

