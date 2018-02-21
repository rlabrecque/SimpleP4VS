// Copyright 2018 Riley Labrecque

namespace SimpleP4VS
{
    using System;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using EnvDTE;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Perforce.P4;

    internal sealed class CheckoutCommandSolutionView
    {
        public static CheckoutCommandSolutionView Instance { get; private set; }

        private readonly Package m_Package;

        private IServiceProvider ServiceProvider { get { return m_Package; } }

        private CheckoutCommandSolutionView(Package package, OleMenuCommandService commandService)
        {
            m_Package = package;

            CommandID id = new CommandID(PackageGuids.guidCheckoutCommandSolutionViewSet, PackageIds.CheckoutCommandSolutionViewId);
            OleMenuCommand cmd = new OleMenuCommand(OnExecute, id);
            commandService.AddCommand(cmd);
        }

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new CheckoutCommandSolutionView(package, commandService);
        }
        
        private void OnExecute(object sender, EventArgs eventArgs)
        {
            // Get the menu that fired the event
            OleMenuCommand menuCommand = sender as OleMenuCommand;
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

            ProjectItem[] projectItems;
            try
            {
                projectItems = GetProjectItems();
                if(projectItems == null)
                {
                    throw new NullReferenceException("GetProjectItem returned null");
                }
            }
            catch(Exception e)
            {
                VsShellUtilities.ShowMessageBox(
                    ServiceProvider,
                    "Could not check out file, GetProjectItem failed.\nException: " + e.Message,
                    "SimpleP4VS - Error: Can not check out file.",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            FileSpec[] filespecs = new FileSpec[projectItems.Length];
            int i = 0;
            foreach (ProjectItem item in projectItems)
            {
                filespecs[i] = new FileSpec(new ClientPath(item.FileNames[0]));
                ++i;
            }

            CheckoutFiles(ServiceProvider, filespecs);
        }

        private ProjectItem[] GetProjectItems()
        {
            IVsMonitorSelection monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            if (monitorSelection == null)
            {
                Debug.Fail("Failed to get SVsShellMonitorSelection service.");
                return null;
            }

            ErrorHandler.ThrowOnFailure(
                monitorSelection.GetCurrentSelection(
                    out IntPtr hierarchyPointer,
                    out uint projectItemId,
                    out IVsMultiItemSelect multiItemSelect,
                    out IntPtr selectionContainerPointer));

            if (selectionContainerPointer != IntPtr.Zero)
            {
                Marshal.Release(selectionContainerPointer);
                selectionContainerPointer = IntPtr.Zero;
            }

            if (projectItemId == (uint)VSConstants.VSITEMID.Selection)
            {
                ErrorHandler.ThrowOnFailure(
                    multiItemSelect.GetSelectionInfo(
                    out uint itemCount,
                    out int fSingleHierarchy));
                
                VSITEMSELECTION[] items = new VSITEMSELECTION[itemCount];
                ErrorHandler.ThrowOnFailure(
                    multiItemSelect.GetSelectedItems(0, itemCount, items));

                ProjectItem[] projectItems = new ProjectItem[itemCount];

                int i = 0;
                foreach (VSITEMSELECTION item in items)
                {
                    ErrorHandler.ThrowOnFailure(
                       item.pHier.GetProperty(
                        item.itemid,
                        (int)__VSHPROPID.VSHPROPID_ExtObject,
                        out object selectedObject));

                    projectItems[i] = selectedObject as ProjectItem;
                    ++i;
                }

                return projectItems;
            }

            // Case where no visible project is open (single file)
            if (hierarchyPointer != IntPtr.Zero)
            {
                IVsHierarchy selectedHierarchy = Marshal.GetUniqueObjectForIUnknown(hierarchyPointer) as IVsHierarchy;

                ErrorHandler.ThrowOnFailure(
                    selectedHierarchy.GetProperty(
                    projectItemId,
                    (int)__VSHPROPID.VSHPROPID_ExtObject,
                    out object selectedObject));

                return new ProjectItem[] { selectedObject as ProjectItem };
            }

            return null;
        }

        private static void CheckoutFiles(IServiceProvider ServiceProvider, FileSpec[] filespecs)
        {
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
            if (!bConnected)
            {
                VsShellUtilities.ShowMessageBox(
                    ServiceProvider,
                    "Connection to Perforce with: P4PORT:{0}, P4USER:{1}, P4CLIENT:{2} failed.",
                    "SimpleP4VS - Error: Could not connect to Perforce.",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            try
            {
                con.Client.EditFiles(new Options(), filespecs);
            }
            finally
            {
                con.Disconnect();
            }
        }
    }
}
