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
            ThreadHelper.ThrowIfNotOnUIThread();

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

            P4Util.CheckoutFiles(ServiceProvider, projectItems);
        }

        private ProjectItem[] GetProjectItems()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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
    }
}
