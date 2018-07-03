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
		internal static void CheckoutFiles(IServiceProvider ServiceProvider, ProjectItem[] projectItems) {
			string P4PORT = Environment.GetEnvironmentVariable("P4PORT");
			string P4USER = Environment.GetEnvironmentVariable("P4USER");
			string P4CLIENT = Environment.GetEnvironmentVariable("P4CLIENT");
			if ((P4PORT == null) || (P4USER == null) || (P4CLIENT == null)) {
				VsShellUtilities.ShowMessageBox(
					ServiceProvider,
					"P4PORT, P4USER, or P4CLIENT have not been set globally. Please set these and try again. See Perforce documentation for more details.",
					"SimpleP4VS - Error: Perforce not set up.",
					OLEMSGICON.OLEMSGICON_WARNING,
					OLEMSGBUTTON.OLEMSGBUTTON_OK,
					OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
				return;
			}

			// Define the server, repository and connection
			Server server = new Server(new ServerAddress(P4PORT));
			Repository rep = new Repository(server);
			Connection con = rep.Connection;
			con.UserName = P4USER;
			con.Client = new Client {
				Name = P4CLIENT
			};

			// Initialize the connection options
			// This information will appear when commands are
			// recorded in the server log as
			// [ProgramName/ProgramVersion]
			Options options = new Options {
				["ProgramName"] = "SimpleP4VS",
				["ProgramVersion"] = "2017.01.18"
			};

			// Connect to the server
			bool bConnected = con.Connect(options);
			if (!bConnected) {
				VsShellUtilities.ShowMessageBox(
					ServiceProvider,
					"Connection to Perforce with: P4PORT:{0}, P4USER:{1}, P4CLIENT:{2} failed.",
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
