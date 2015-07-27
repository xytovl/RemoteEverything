using System;
using UnityEngine;

namespace RemoteEverything
{
	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class Plugin : MonoBehaviour
	{
		static HttpServer httpServer;
		static ApplicationLauncherButton toolbarButton;
		static Texture2D inactiveTexture;
		static Texture2D activeTexture;

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
				toggleServer, toggleServer,
				null, null,
				null, null,
				ApplicationLauncher.AppScenes.ALWAYS,
				httpServer == null ? inactiveTexture : activeTexture);
		}

		static void toggleServer()
		{
			if (httpServer == null)
			{
				httpServer = new HttpServer(8080);
				toolbarButton.SetTexture(activeTexture);
			}
			else
			{
				httpServer.terminate();
				httpServer = null;
				toolbarButton.SetTexture(inactiveTexture);
			}
		}
		
	}
}

