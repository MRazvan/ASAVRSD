using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Debugger.Server.Transports;
using Microsoft.VisualStudio.Shell;
using SoftwareDebuggerExtension.SDebugger;
using Microsoft.VisualStudio;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Atmel.Studio.Services;

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
        private SimulatorDebugger _debugger;
        private Commands _commands;
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
            _output = new Output(this);
            _output.Initialize();
            //Trace.Listeners.Add(new VSOutWindowListener(_output));
            _debugger = new SimulatorDebugger(this, _output);
            _debugger.DebugStateChanged += () => _commands.SetDebugState(_debugger.CanRun);
            _dte = GetService(typeof(SDTE)) as DTE;
        }

        private void ContinueCallback()
        {
            _debugger.Continue();
        }

        private void StepCallback()
        {
            _debugger.Step();
        }

        private static string AVRDude = @"D:\arduino-1.8.1\hardware\tools\avr\bin\avrdude.exe";
        private static string AVRDudeConfig = @"D:\\arduino-1.8.1\\hardware\\tools\\avr\\etc\\avrdude.conf";
        private static string ProjectOutput = @"D:\\GIT\\ASAVRSD\\DebuggerLib\\DebugClient\\Debug\\DebugClient.hex";

        private void RunAndAttachCallback()
        {
            // Start a build / upload the program, start the debugger and reset the target, 4 steps to debug :|
            _output.Activate(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid);
            _dte.Solution.SolutionBuild.Build(true);


            _output.Activate(Output.SDDebugOutputPane);
            // Upload the program
            var prog = new System.Diagnostics.Process();
            prog.StartInfo = new ProcessStartInfo
            {
                FileName = AVRDude,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = $"\"-C{AVRDudeConfig}\" -v -patmega328p -carduino -P{_commands.Port} -b57600 -D -Uflash:w:\"{ProjectOutput}\":i"
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