using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using SoftwareDebuggerExtension.ExtensionConfiguration;

namespace SoftwareDebuggerExtension
{
    internal class SolutionEventsListener : IVsSolutionEvents
    {
        private readonly IServiceProvider _serviceProvider;
        private uint _cookie;

        public SolutionEventsListener(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return 0;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return 0;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return 0;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return 0;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return 0;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return 0;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
            Settings.Instance.LoadSolutionSettings(dte.Solution);
            Settings.Instance.SolutionSettingsLoaded = true;
            return 0;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return 0;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return 0;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            Settings.Instance.SolutionSettingsLoaded = false;
            return 0;
        }

        public void Register()
        {
            var solutionService = _serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            solutionService?.AdviseSolutionEvents(this, out _cookie);
        }
    }
}