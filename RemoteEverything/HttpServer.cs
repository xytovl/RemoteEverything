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
		string documentRoot = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "www");
		Dictionary<string, string> contentTypes = new Dictionary<string, string>()
		{
			{ ".html", "text/html; charset=utf-8" },
			{ ".css", "text/css" },
			{ ".js", "text/javascript" }
		};

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
				ServeFile(ctx);
				return;
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

		void ServeFile(HttpListenerContext ctx)
		{
			string uri = ctx.Request.Url.AbsolutePath;
			if (uri.EndsWith("/"))
				uri += "index.html";

			string[] components = uri.Split(new char[]{ '/' }, StringSplitOptions.RemoveEmptyEntries);

			if (components.Any(s => s == ".."))
				throw new HttpException(400);

			string path = System.IO.Path.Combine(documentRoot, String.Join(System.IO.Path.DirectorySeparatorChar.ToString(), components));

			#if DEBUG
			Debug.Log("File name: " + path);
			#endif

			string contentType;
			if (!contentTypes.TryGetValue(System.IO.Path.GetExtension(path), out contentType))
				throw new HttpException(404);
			ctx.Response.ContentType = contentType;

			try
			{
				var f = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				ctx.Response.ContentLength64 = f.Length;
				int n = 0;
				byte[] buffer = new byte[32768];
				while ((n = f.Read(buffer, 0, buffer.Length)) > 0)
				{
					ctx.Response.OutputStream.Write(buffer, 0, n);
				}
				f.Close();
			}
			catch(Exception e)
			{
				Debug.LogException(e);
				throw new HttpException(404);
			}
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

			if (details.AsDouble != null)
			{
				content.Add("value", Json.Node.MakeValue(details.AsDouble(obj)));
			}
			else if (details.AsString != null)
			{
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

