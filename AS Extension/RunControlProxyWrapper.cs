using Atmel.VsIde.AvrStudio.Services.TargetService.TCF.Services.RunControl;
using System.Collections.Generic;
using Atmel.Studio.Services;
using Microsoft.VisualStudio.Shell.Interop;

namespace SoftwareDebuggerExtension
{
    class RunControlProxyWrapper : IRunControlContext
    {
        private IRunControlContext _context;
        private IVsOutputWindowPane _traceOutPane;

        public RunControlProxyWrapper(IRunControlContext context)
        {
            _context = context;
        }

        public RunControlProxyWrapper(IRunControlContext context, IVsOutputWindowPane _traceOutPane) : this(context)
        {
            this._traceOutPane = _traceOutPane;
        }

        public bool CanCount(int mode)
        {
            return _context.CanCount(mode);
        }

        public bool CanResume(int mode)
        {
            return _context.CanResume(mode);
        }

        public bool CanSuspend()
        {
            return _context.CanSuspend();
        }

        public bool CanTerminate()
        {
            return _context.CanTerminate();
        }

        public string GetID()
        {
            return _context.GetID();
        }

        public string GetParentID()
        {
            return _context.GetParentID();
        }

        public Dictionary<string, object> GetProperties()
        {
            return _context.GetProperties();
        }

        public bool HasState()
        {
            return _context.HasState();
        }

        public bool IsContainer()
        {
            return _context.IsContainer();
        }

        public bool Resume(int mode, int count, Dictionary<string, object> parms)
        {
            _traceOutPane.OutputString($"Resume mode {mode}, count {count}\n");
            return _context.Resume(mode, count, parms);
        }

        public bool Resume(int mode, ulong arg, out IStatus status)
        {
            _traceOutPane.OutputString($"Resume mode {mode}, arg {arg}\n");
            return _context.Resume(mode, arg, out status);
        }

        public bool SetProperties(Dictionary<string, object> properties, out IStatus status)
        {
            return _context.SetProperties(properties, out status);
        }

        public bool Suspend(out IStatus status)
        {
            _traceOutPane.OutputString($"Suspend\n");
            return _context.Suspend(out status);
        }

        public bool Terminate(out IStatus status)
        {
            _traceOutPane.OutputString($"Terminate\n");
            return _context.Terminate(out status);
        }
    }
}
