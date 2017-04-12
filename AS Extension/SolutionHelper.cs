using System;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SoftwareDebuggerExtension.ExtensionConfiguration;

namespace SoftwareDebuggerExtension
{
    internal class SolutionHelper : IVsSolutionEvents
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Commands _commands;
        private uint _cookie;

        public SolutionHelper(IServiceProvider serviceProvider, Commands commands)
        {
            _serviceProvider = serviceProvider;
            _commands = commands;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            UpdateProjectsSettings();
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
            Settings.Instance.LoadSolutionSettings(dte.Solution);
            Settings.Instance.SolutionSettingsLoaded = true;
            _commands.EnableUpload();
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            Settings.Instance.SolutionSettingsLoaded = false;
            _commands.DisableUpload();
            return VSConstants.S_OK;
        }

        public void Register()
        {
            var solutionService = _serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            solutionService?.AdviseSolutionEvents(this, out _cookie);
        }

        public void UpdateProjectsSettings()
        {
            var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
            foreach (Project proj in dte.Solution.Projects)
            {
                var pi = new ProjectInfo(proj);
                if (!pi.IsValid)
                    return;
                pi.AddDefines(Settings.Instance.SolutionSettings.Options, Settings.DebuggingCaps);
            }
        }
    }
}