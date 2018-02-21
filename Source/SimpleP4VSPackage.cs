// Copyright 2018 Riley Labrecque

namespace SimpleP4VS
{
    using System;
    using System.ComponentModel.Design;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Task = System.Threading.Tasks.Task;

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    //[ProvideOptionPage(typeof(Options), "Web", Vsix.Name, 101, 102, true, new string[0], ProvidesLocalizedCategoryName = false)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidSimpleP4VSPackageString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string)]
    public sealed class SimpleP4VSPackage : AsyncPackage
    {
        //public static Options Options { get; private set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<Microsoft.VisualStudio.Shell.ServiceProgressData> progress)
        {
            //Options = (Options)GetDialogPage(typeof(Options));
            var commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (commandService != null)
            {
                CheckoutCommand.Initialize(this, commandService);
            }
        }
    }
}
