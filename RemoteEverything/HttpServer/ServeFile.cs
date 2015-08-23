using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEngine;

namespace RemoteEverything
{
	public static class ServeFile
	{
		static readonly string documentRoot = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "www");
		static readonly Dictionary<string, string> contentTypes = new Dictionary<string, string>()
		{
			{ ".html", "text/html; charset=utf-8" },
			{ ".css", "text/css" },
			{ ".js", "text/javascript" }
		};

		public static void Serve(HttpListenerContext ctx)
		{
			string uri = ctx.Request.Url.AbsolutePath;
			if (uri.EndsWith("/"))
				uri += "index.html";

			string[] components = uri.Split(new char[]{ '/' }, StringSplitOptions.RemoveEmptyEntries);

			if (components.Any(s => s == ".."))
				throw new HttpServer.HttpException(400);

			string path = System.IO.Path.Combine(documentRoot, String.Join(System.IO.Path.DirectorySeparatorChar.ToString(), components));

			#if DEBUG
			Debug.Log("File name: " + path);
			#endif

			string contentType;
			if (!contentTypes.TryGetValue(System.IO.Path.GetExtension(path), out contentType))
				throw new HttpServer.HttpException(404);
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
				throw new HttpServer.HttpException(404);
			}
		}
	}
}

