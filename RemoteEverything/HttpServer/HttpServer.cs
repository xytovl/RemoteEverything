using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RemoteEverything
{
	public class HttpServer
	{

		public class HttpException: Exception
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
				ListRequest.HandleListRequest(outputStream, ctx.Request);
				break;
			default:
				ServeFile.Serve(ctx);
				return;
			}

			outputStream.Flush();
			ctx.Response.Headers.Add("Access-Control-Allow-Origin: *");
			ctx.Response.ContentLength64 = memStream.Length;
			memStream.WriteTo(ctx.Response.OutputStream);
		}


	}
}

