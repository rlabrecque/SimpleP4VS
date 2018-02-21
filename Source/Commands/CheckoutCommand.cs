// Copyright 2018 Riley Labrecque

namespace SimpleP4VS
{
    using System;
    using System.ComponentModel.Design;
    using System.Runtime.InteropServices;
    using EnvDTE;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Perforce.P4;

    internal sealed class CheckoutCommand
    {
        public static CheckoutCommand Instance { get; private set; }

        private readonly Package m_Package;

        private IServiceProvider ServiceProvider { get { return m_Package; } }

        private CheckoutCommand(Package package, OleMenuCommandService commandService)
        {
            m_Package = package;

            var id = new CommandID(PackageGuids.guidCheckoutCommandSet, PackageIds.CheckoutCommandId);
            var cmd = new OleMenuCommand(OnExecute, id);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);
        }

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new CheckoutCommand(package, commandService);
        }

        void BeforeQueryStatus(object sender, EventArgs e)
        {
            //var button = (OleMenuCommand)sender;

            //button.Checked = VSPackage.Options.EnableReload;

            VsShellUtilities.LogMessage("Test", "BeginQueryStatus !!!!!", __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION);
        }

        private void OnExecute(object sender, EventArgs e)
        {
            // get the menu that fired the event
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                VsShellUtilities.ShowMessageBox(
                    ServiceProvider,
                    "menuCommand was null.",
                    "SimpleP4VS - Error: Could not check out file.",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                return;
            }

            ProjectItem projectItem;
            try
            {
                projectItem = GetProjectItem();
            }
            catch
            {
                VsShellUtilities.ShowMessageBox(
                    ServiceProvider,
                    "Could not check out file, GetProjectItem failed.",
                    "SimpleP4VS - Error: Can not check out file.",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            string P4PORT = Environment.GetEnvironmentVariable("P4PORT");
            string P4USER = Environment.GetEnvironmentVariable("P4USER");
            string P4CLIENT = Environment.GetEnvironmentVariable("P4CLIENT");
            if ((P4PORT == null) || (P4USER == null) || (P4CLIENT == null))
            {
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
            con.Client = new Client
            {
                Name = P4CLIENT
            };

            // Initialize the connection options
            // This information will appear when commands are
            // recorded in the server log as
            // [ProgramName/ProgramVersion]
            Options options = new Options
            {
                ["ProgramName"] = "SimpleP4VS",
                ["ProgramVersion"] = "2017.01.18"
            };

            // Connect to the server
            bool bConnected = con.Connect(options);
            if(!bConnected)
            {
                VsShellUtilities.ShowMessageBox(
                    this.ServiceProvider,
                    "Connection to Perforce with: P4PORT:{0}, P4USER:{1}, P4CLIENT:{2} failed.",
                    "SimpleP4VS - Error: Could not connect to Perforce.",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            try
            {
                con.Client.EditFiles(new Options(), new FileSpec[] { new FileSpec(new ClientPath(projectItem.FileNames[0])) });
            }
            finally
            {
                con.Disconnect();
            }
        }

        private static ProjectItem GetProjectItem()
        {
            IVsMonitorSelection monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;

            monitorSelection.GetCurrentSelection(out IntPtr hierarchyPointer, out uint projectItemId, out IVsMultiItemSelect multiItemSelect, out IntPtr selectionContainerPointer);

            Object selectedObject = null;
            if (Marshal.GetTypedObjectForIUnknown(hierarchyPointer, typeof(IVsHierarchy)) is IVsHierarchy selectedHierarchy)
            {
                ErrorHandler.ThrowOnFailure(selectedHierarchy.GetProperty((uint)VSConstants.VSITEMID.Selection, (int)__VSHPROPID.VSHPROPID_ExtObject, out selectedObject));
            }

            return selectedObject as ProjectItem;
        }
    }
}
