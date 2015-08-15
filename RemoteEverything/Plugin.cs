using System;
using UnityEngine;

namespace RemoteEverything
{
	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class Plugin : MonoBehaviour
	{
		static HttpServer httpServer;
		static ushort port = 8080;

		static ApplicationLauncherButton toolbarButton;
		static Texture2D inactiveTexture;
		static Texture2D activeTexture;

		static bool windowVisible;
		static Rect windowPosition;
		static int windowId = GUIUtility.GetControlID(FocusType.Native);
		static string portString = "8080";

		static System.Diagnostics.Stopwatch doubleClick = new System.Diagnostics.Stopwatch();

		static bool httpServerStarted { get { return httpServer != null;}}

		public Plugin ()
		{
			GameEvents.onGUIApplicationLauncherReady.Add(addButton);
			addButton();
		}

		void addButton()
		{
			if (ApplicationLauncher.Instance == null)
				return;
			if (toolbarButton != null)
				return;

			if (inactiveTexture == null)
				inactiveTexture = GameDatabase.Instance.GetTexture("RemoteEverything/icon38-inactive", false);
			if (activeTexture == null)
				activeTexture = GameDatabase.Instance.GetTexture("RemoteEverything/icon38-active", false);

			toolbarButton = ApplicationLauncher.Instance.AddModApplication(
				showWindow, hideWindow,
				null, null,
				null, null,
				ApplicationLauncher.AppScenes.ALWAYS,
				httpServer == null ? inactiveTexture : activeTexture);
		}

		static void toggleServer()
		{
			if (httpServerStarted)
			{
				httpServer.Terminate();
				httpServer = null;
				toolbarButton.SetTexture(inactiveTexture);
			}
			else
			{
				httpServer = new HttpServer(port);
				toolbarButton.SetTexture(activeTexture);
			}
		}

		static void showWindow()
		{
			windowVisible = true;
			doubleClick = System.Diagnostics.Stopwatch.StartNew();
		}

		static void hideWindow()
		{
			if (doubleClick.ElapsedMilliseconds < 400)
				toggleServer();
			windowVisible = false;
		}

		public void Update()
		{
			if (httpServer != null)
				httpServer.ProcessRequests();
		}

		public void OnGUI()
		{
			if (!windowVisible)
				return;
			windowPosition.width = Math.Max(windowPosition.width, 300);
			windowPosition = GUILayout.Window(windowId, windowPosition,
					drawWindow, "RemoteEverything");
		}

		void drawWindow(int windowId)
		{
			GUILayout.Label("Activating the server will allow network connections from other devices, possibly from the internet");
			if (GUILayout.Toggle(httpServerStarted, "Server active") != httpServerStarted)
			{
				try
				{
					toggleServer();
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
			GUILayout.BeginHorizontal();
			GUILayout.Label("port: ");
			if (httpServerStarted)
			{
				GUILayout.Label(port.ToString());
			}
			else
			{
				portString = GUILayout.TextField(portString);
				ushort.TryParse(portString, out port);
			}
			GUILayout.EndHorizontal();
			GUILayout.Label("");
			GUILayout.Label("Hint: you can toggle the server by double-clicing on the icon when the window is closed");

			GUI.DragWindow();
		}
		
	}
}

