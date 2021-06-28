// Copyright 2018 Riley Labrecque
namespace SimpleP4VS {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using EnvDTE;
	using Microsoft.VisualStudio;
	using Microsoft.VisualStudio.Shell;
	using Microsoft.VisualStudio.Shell.Interop;
	using Perforce.P4;

	internal static class P4Util {
		internal static void CheckoutFiles(IServiceProvider ServiceProvider, ProjectItem[] projectItems)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (projectItems == null || projectItems.Length == 0)
			{
				VsShellUtilities.ShowMessageBox(
					ServiceProvider,
					"No project items passed.",
					"SimpleP4VS - Error: Checkout failed",
					OLEMSGICON.OLEMSGICON_WARNING,
					OLEMSGBUTTON.OLEMSGBUTTON_OK,
					OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
				return;
			}

			string fileDirectory = System.IO.Path.GetDirectoryName(projectItems[0].FileNames[0]);

			// Define the server, repository and connection
			P4Server p4server = new P4Server(fileDirectory);
			Server server = new Server(new ServerAddress(p4server.Port));
			Repository rep = new Repository(server);
			Connection con = rep.Connection;

			// Initialize the connection options
			// This information will appear when commands are
			// recorded in the server log as
			// [ProgramName/ProgramVersion]
			Options options = new Options {
				["ProgramName"] = "SimpleP4VS",
				["ProgramVersion"] = "2017.01.18",
				["cwd"] = fileDirectory
			};

			// Connect to the server
			bool bConnected = con.Connect(options);
			if (!bConnected) {
				VsShellUtilities.ShowMessageBox(
					ServiceProvider,
					"Connection to Perforce with failed.",
					"SimpleP4VS - Error: Could not connect to Perforce.",
					OLEMSGICON.OLEMSGICON_WARNING,
					OLEMSGBUTTON.OLEMSGBUTTON_OK,
					OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
				return;
			}

			FileSpec[] filespecs = new FileSpec[projectItems.Length];
			int i = 0;
			foreach (ProjectItem item in projectItems) {
				filespecs[i] = new FileSpec(new ClientPath(item.FileNames[0]));
				++i;
			}


			try {
				con.Client.EditFiles(new Options(), filespecs);
			}
			finally {
				con.Disconnect();
			}
		}
	}
}
