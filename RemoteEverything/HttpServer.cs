using System;
using System.Net;
using UnityEngine;

namespace RemoteEverything
{
	public class HttpServer
	{

		class HttpError : Exception
		{
			public int code;
			public HttpError(int code)
			{
				this.code = code;
			}
		}

		HttpListener listener;
		
		public HttpServer(ushort port)
		{
			listener = new HttpListener();
			listener.Prefixes.Add(string.Format("http://*:{0}/", port));
			listener.Start();
			listener.BeginGetContext(chainRequests, null);
		}

		public void terminate()
		{
			listener.Close();
		}

		void chainRequests(IAsyncResult result)
		{
			HttpListenerContext ctx = null;
			try
			{
				ctx = listener.EndGetContext(result);
				handleRequest(ctx);
				ctx.Response.Close();
			}
			catch (Exception e)
			{
				if (ctx != null)
				{
					var httpError = e as HttpError;
					if (e != null)
					{
						ctx.Response.StatusCode = httpError.code;
					}
					ctx.Response.Close();
				}
				Debug.LogException(e);
			}
			if (listener.IsListening)
				listener.BeginGetContext(chainRequests, null);
		}

		void handleRequest(HttpListenerContext ctx)
		{
			#if DEBUG
			Debug.Log("Handling request " + ctx.Request.RawUrl);
			#endif

			var uri = ctx.Request.Url;

			if (uri == null)
				throw new HttpError(400);

			#if DEBUG
			Debug.Log("Path: " + uri.AbsolutePath);
			#endif

			var memStream = new System.IO.MemoryStream();
			var outputStream = new System.IO.StreamWriter(memStream);

			switch(uri.AbsolutePath)
			{
			case "/list":
				handleListRequest(outputStream);
				break;
			default:
				throw new HttpError(404);
			}
			outputStream.Flush();
			Debug.Log(string.Format("content length: {0}", memStream.Length));
			ctx.Response.ContentLength64 = memStream.Length;
			memStream.WriteTo(ctx.Response.OutputStream);
		}

		void handleListRequest(System.IO.TextWriter result)
		{
			#if DEBUG
			Debug.Log("Processing list request");
			#endif
			result.WriteLine("Content list:");
			RemotableContainer.instance.walk(obj => printObjectDescription(obj, result));
		}

		static void printObjectDescription(object obj, System.IO.TextWriter stream)
		{
			var content = RemotableContent.get(obj.GetType());

			foreach (var kv in content.exported)
			{
				stream.Write(kv.Key);
			}

		}
	}
}

