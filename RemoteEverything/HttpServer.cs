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

		void HandleListRequest(System.IO.TextWriter result)
		{
			#if DEBUG
			Debug.Log("Processing list request");
			#endif
			result.Write("{\"objects\":[");
			RemotableContainer.Instance.Walk((id, obj) => PrintObjectDescription(id, obj, result));
			result.Write("]}");
		}

		static void PrintObjectDescription(int id, object obj, System.IO.TextWriter stream)
		{
			stream.Write("{\"id\":");
			stream.Write(id);
			stream.Write(",\"type\":\"");
			stream.Write(JsonEscape(obj.GetType().FullName));
			stream.Write("\",\"content\":{");
			var content = RemotableContent.get(obj.GetType());

			bool first = true;

			foreach (var kv in content.exported)
			{
				if (first)
					first = false;
				else
					stream.Write(",");
				PrintMember(kv.Value, obj, stream);
			}
			stream.WriteLine("}");

		}

		static void PrintMember(MemberInfo info, object obj, System.IO.TextWriter stream)
		{
			stream.Write('"');
			stream.Write(JsonEscape(info.Name));
			stream.Write("\":{\"type\":");
			if (info is FieldInfo)
				stream.Write("field");
			else if (info is PropertyInfo)
				stream.Write("property");
			else if (info is MethodInfo)
				stream.Write("method");
			else
				stream.Write("unknown");
			stream.Write("\"");
			if (IsPrintable(info))
			{
				stream.Write("\"value\":");
				PrintValue(info, obj, stream);
			}
			stream.Write("}");
		}

		static bool IsPrintable(MemberInfo info)
		{
			var fieldInfo = info as FieldInfo;
			if (fieldInfo != null)
			{
				var type = fieldInfo.FieldType;
				return type == typeof(string)
					|| type == typeof(double);
			}
			return false;
		}

		static void PrintValue(MemberInfo info, object obj, System.IO.TextWriter stream)
		{
			var fieldInfo = info as FieldInfo;
			if (fieldInfo != null)
			{
				var val = fieldInfo.GetValue(obj);
				if (fieldInfo.FieldType == typeof(string))
				{
					PrintValue(val as string, stream);
					return;
				}
				if (fieldInfo.FieldType == typeof(double))
				{
					PrintValue((double)val, stream);
					return;
				}
			}
			Debug.Log(string.Format("failed to print value of {0}", info));
		}

		static void PrintValue(string val, System.IO.TextWriter stream)
		{
			stream.Write('"');
			stream.Write(JsonEscape(val));
			stream.Write('"');
		}
		static void PrintValue(double val, System.IO.TextWriter stream)
		{
			stream.Write(val);
		}

		static string JsonEscape(string str)
		{
			return str.Replace("\"", "\\\"").Replace("\\", "\\\\");
		}
	}
}

