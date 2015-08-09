using System;
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

		public HttpServer(ushort port)
		{
			listener = new HttpListener();
			listener.Prefixes.Add(string.Format("http://*:{0}/", port));
			listener.Start();
			listener.BeginGetContext(ChainRequests, null);
		}

		public void Terminate()
		{
			listener.Close();
		}

		void ChainRequests(IAsyncResult result)
		{
			HttpListenerContext ctx = null;
			try
			{
				ctx = listener.EndGetContext(result);
				HandleRequest(ctx);
				ctx.Response.Close();
			}
			catch (Exception e)
			{
				if (ctx != null)
				{
					var httpError = e as HttpException;
					if (httpError != null)
						ctx.Response.StatusCode = httpError._code;
					ctx.Response.Close();
				}
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
			ctx.Response.ContentLength64 = memStream.Length;
			memStream.WriteTo(ctx.Response.OutputStream);
		}

		void HandleListRequest(System.IO.TextWriter stream)
		{
			#if DEBUG
			Debug.Log("Processing list request");
			#endif
			var result = new Json.Object();
			var objects = new Json.List();
			RemotableContainer.Instance.Walk((id, obj) => objects.Add(BuildObjectDescription(id, obj)));
			result.Add("objects", objects);
			result.Write(stream);
		}

		static Json.Node BuildObjectDescription(int id, object obj)
		{
			var type = obj.GetType();
			var description = new Json.Object();
			description.Add("id", Json.Node.MakeValue(id));
			description.Add("type", Json.Node.MakeValue(type.FullName));

			var content = new Json.Object();
			foreach (var kv in RemotableContent.get(type).exported)
				content.Add(kv.Value.Name, BuildMember(kv.Value, obj));

			description.Add("content", content);
			return description;
		}

		static Json.Node BuildMember(MemberInfo info, object obj)
		{
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

