using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using Atmel.Studio.Services;
using Debugger.Server.Transports;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SoftwareDebuggerExtension.SDebugger;
using Process = System.Diagnostics.Process;

namespace SoftwareDebuggerExtension
{
    /// <summary>
    ///     This is the class that implements the package exposed by this assembly.
    ///     The minimum requirement for a class to be considered a valid package for Visual Studio
    ///     is to implement the IVsPackage interface and register itself with the shell.
    ///     This package uses the helper classes defined inside the Managed Package Framework (MPF)
    ///     to do it: it derives from the Package class that provides the implementation of the
    ///     IVsPackage interface and uses the registration attributes defined in the framework to
    ///     register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidSoftwareDebuggerPkgString)]
    public sealed class SoftwareDebuggerPackage : Package
    {
        private Commands _commands;
        private SimulatorDebugger _debugger;
        private DTE _dte;
        private Output _output;

        /// <summary>
        ///     Default constructor of the package.
        ///     Inside this method you can place any initialization code that does not require
        ///     any Visual Studio service because at this point the package object is created but
        ///     not sited yet inside Visual Studio environment. The place to do all the other
        ///     initialization is the Initialize method.
        /// </summary>
        public SoftwareDebuggerPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation

        #region Package Members

        /// <summary>
        ///     Initialization of the package; this method is called right after the package is sited, so this is the place
        ///     where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();
            // Add our command handlers for menu (commands must exist in the .vsct file)
            _commands = new Commands(this);
            _commands.Initialize();
            _commands.RunAndAttach += RunAndAttachCallback;
            _commands.Step += StepCallback;
            _commands.Continue += ContinueCallback;
            _commands.ShowOptions += ShowOptions;
            _output = new Output(this);
            _output.Initialize();
            Trace.Listeners.Add(new VSTraceListener(_output));
            _debugger = new SimulatorDebugger(this, _output);
            _debugger.DebugStateChanged += () => _commands.SetDebugState(_debugger.CanRun);
            _dte = GetService(typeof(SDTE)) as DTE;
        }

        private void ShowOptions()
        {
            var settings = new Settings();
            settings.SetArduinoPath(string.Empty);
            settings.SetProjectDefines(new List<string> {"CAPS_SAVE_CTX"});
            settings.Owner = Application.Current.MainWindow;
            settings.ShowDialog();
        }

        private void ContinueCallback()
        {
            _debugger.Continue();
        }

        private void StepCallback()
        {
            _debugger.Step();
        }

        private string GetArduinoIDE()
        {
            return @"D:\arduino-1.8.1\";
        }

        private void Alert(string text, string header)
        {
            ATServiceProvider.DialogService.ShowDialog(null, text, header, DialogButtonSet.Ok, DialogIcon.Exclamation);
        }

        private void RunAndAttachCallback()
        {
            var proj = _dte.ActiveSolutionProjects[0] as Project;
            if (proj == null)
            {
                Alert("There is no active project", "Error");
                return;
            }

            var pi = new ProjectInfo(proj);
            if (string.IsNullOrWhiteSpace(pi.OutputPath))
            {
                Alert("The active project has no output path", "Error");
                return;
            }
            if (!pi.IsExecutable)
            {
                Alert("The active project is not executable", "Error");
                return;
            }
            if (pi.ToolName != "Simulator")
            {
                Alert("The active project must have Simulator selected as a tool", "Error");
                return;
            }
            if (string.IsNullOrWhiteSpace(_commands.Port))
            {
                Alert("No com port selected\nPlease select a com port and try again", "Error");
                return;
            }

            // Start a build / upload the program, start the debugger and reset the target, 4 steps to debug :|
            _output.Activate(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid);
            _dte.Solution.SolutionBuild.Build(true);


            _output.Activate(Output.SDDebugOutputPane);
            // Upload the program
            var prog = new Process();
            prog.StartInfo = new ProcessStartInfo
            {
                FileName = $"{GetArduinoIDE()}hardware\\tools\\avr\\bin\\avrdude.exe",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments =
                    $"\"-C{GetArduinoIDE()}hardware\\tools\\avr\\etc\\avrdude.conf\" -v -p{pi.Device?.Name.ToLower()} -carduino -P{_commands.Port} -b57600 -D -Uflash:w:\"{pi.OutputPath}\":i"
            };
            prog.EnableRaisingEvents = true;
            prog.OutputDataReceived += (sender, e) => _output.DebugOutLine(e.Data);
            prog.ErrorDataReceived += (sender, e) => _output.DebugOutLine(e.Data);
            prog.Start();
            prog.BeginErrorReadLine();
            prog.BeginOutputReadLine();
            prog.WaitForExit();

            if (prog.ExitCode == 0)
            {
                ATServiceProvider.EventsService.DebugActionChanged += EventsService_DebugActionChanged;
                _dte.ExecuteCommand("Debug.Start");
            }
        }

        private void EventsService_DebugActionChanged(object sender, DebugEventArgs e)
        {
            _output.DebugOutLine(e.DebugAction.ToString());
            if (e.DebugAction == DebugAction.Launched)
            {
                StartDebug();
            }
            else if (e.DebugAction == DebugAction.Detaching)
            {
                ATServiceProvider.EventsService.DebugActionChanged -= EventsService_DebugActionChanged;
                _debugger.Stop();
            }
        }

        private void StartDebug()
        {
            var transport = new SerialTransport();
            transport.SetPort(_commands.Port);
            transport.SetSpeed(_commands.BaudRate);
            _debugger.Start(transport);

            _debugger.ResetTarget();
        }

        #endregion
    }
}