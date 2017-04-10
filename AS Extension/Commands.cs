using System;
using System.ComponentModel.Design;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace SoftwareDebuggerExtension
{
    public class Commands
    {
        public delegate void ContinueDelegate();

        public delegate void RunAndAttachDelegate();

        public delegate void ShowOptionsDelegate();

        public delegate void StepDelegate();

        private readonly IServiceProvider _serviceProvider;

        private readonly string[] _bauds = {"9600", "14400", "19200", "28800", "38400", "57600", "115200", "500000"};
        private OleMenuCommand _continueCommand;
        private string[] _ports;
        private int _selectedBaud;
        private string _selectedBaudString;
        private OleMenuCommand _stepCommand;

        public Commands(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _selectedBaud = 500000;
            Port = SerialPort.GetPortNames().FirstOrDefault();
        }

        public int BaudRate => _selectedBaud;
        public string Port { get; private set; }

        public event RunAndAttachDelegate RunAndAttach;
        public event StepDelegate Step;
        public event ContinueDelegate Continue;
        public event ShowOptionsDelegate ShowOptions;

        public void Initialize()
        {
            var mcs = _serviceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                mcs.AddCommand(new OleMenuCommand(AttachCallback,
                    new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int) PkgCmdIDList.cmdAttach)));
                _stepCommand = new OleMenuCommand(StepCallback,
                    new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int) PkgCmdIDList.cmdStep));
                mcs.AddCommand(_stepCommand);

                _continueCommand = new OleMenuCommand(ContinueCallback,
                    new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int) PkgCmdIDList.cmdContinue));
                mcs.AddCommand(_continueCommand);

                mcs.AddCommand(new OleMenuCommand(OnPortDropDownCombo,
                    new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int) PkgCmdIDList.cmdSelectPort)));
                mcs.AddCommand(new OleMenuCommand(OnPortDropDownComboList,
                    new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int) PkgCmdIDList.cmdSelectPortList)));

                mcs.AddCommand(new OleMenuCommand(OnBaudDropDownCombo,
                    new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int) PkgCmdIDList.cmdSelectBaud)));
                mcs.AddCommand(new OleMenuCommand(OnBaudDropDownComboList,
                    new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int) PkgCmdIDList.cmdSelectBaudList)));

                mcs.AddCommand(new OleMenuCommand(OnOptions,
                    new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int) PkgCmdIDList.cmdOptions)));
            }
            _continueCommand.Enabled = _stepCommand.Enabled = false;
        }

        private void OnOptions(object sender, EventArgs e)
        {
            ShowOptions?.Invoke();
        }

        public void SetDebugState(bool inDebug)
        {
            _stepCommand.Enabled = _continueCommand.Enabled = inDebug;
        }

        private void ContinueCallback(object sender, EventArgs e)
        {
            Continue?.Invoke();
        }

        private void StepCallback(object sender, EventArgs e)
        {
            Step?.Invoke();
        }

        private void AttachCallback(object sender, EventArgs e)
        {
            RunAndAttach?.Invoke();
        }

        private void OnPortDropDownComboList(object sender, EventArgs e)
        {
            var eventArgs = e as OleMenuCmdEventArgs;
            _ports = SerialPort.GetPortNames();
            if (Port == null)
                Port = _ports.FirstOrDefault();
            if (eventArgs != null)
            {
                var inParam = eventArgs.InValue;
                var vOut = eventArgs.OutValue;

                if (inParam != null)
                    throw new ArgumentException(Resources.InParamIllegal); // force an exception to be thrown
                if (vOut != IntPtr.Zero)
                    Marshal.GetNativeVariantForObject(_ports, vOut);
                else
                    throw new ArgumentException(Resources.OutParamRequired); // force an exception to be thrown
            }
        }

        private void OnPortDropDownCombo(object sender, EventArgs e)
        {
            var eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null)
            {
                var newChoice = eventArgs.InValue as string;
                var vOut = eventArgs.OutValue;

                if (vOut != IntPtr.Zero)
                {
                    // when vOut is non-NULL, the IDE is requesting the current value for the combo
                    Marshal.GetNativeVariantForObject(Port, vOut);
                }

                else if (newChoice != null)
                {
                    // new value was selected or typed in
                    // see if it is one of our items
                    var validInput = false;
                    var indexInput = -1;
                    for (indexInput = 0; indexInput < _ports.Length; indexInput++)
                        if (string.Compare(_ports[indexInput], newChoice, StringComparison.CurrentCultureIgnoreCase) ==
                            0)
                        {
                            validInput = true;
                            break;
                        }

                    if (validInput)
                        Port = _ports[indexInput];
                    else
                        throw new ArgumentException(Resources.ParamNotValidStringInList);
                            // force an exception to be thrown
                }
            }
            else
            {
                // We should never get here; EventArgs are required.
                throw new ArgumentException(Resources.EventArgsRequired); // force an exception to be thrown
            }
        }

        private void OnBaudDropDownComboList(object sender, EventArgs e)
        {
            var eventArgs = e as OleMenuCmdEventArgs;
            if (_selectedBaudString == null)
            {
                _selectedBaudString = "500000";
                _selectedBaud = 500000;
            }
            if (eventArgs != null)
            {
                var inParam = eventArgs.InValue;
                var vOut = eventArgs.OutValue;

                if (inParam != null)
                    throw new ArgumentException(Resources.InParamIllegal); // force an exception to be thrown
                if (vOut != IntPtr.Zero)
                    Marshal.GetNativeVariantForObject(_bauds, vOut);
                else
                    throw new ArgumentException(Resources.OutParamRequired); // force an exception to be thrown
            }
        }

        private void OnBaudDropDownCombo(object sender, EventArgs e)
        {
            if (null == e || e == EventArgs.Empty)
                throw new ArgumentException(Resources.EventArgsRequired); // force an exception to be thrown

            var eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null)
            {
                var input = eventArgs.InValue;
                var vOut = eventArgs.OutValue;

                if (vOut != IntPtr.Zero && input != null)
                    throw new ArgumentException(Resources.BothInOutParamsIllegal); // force an exception to be thrown
                if (vOut != IntPtr.Zero)
                    if (_selectedBaudString == null)
                        Marshal.GetNativeVariantForObject("500000", vOut);
                    else
                        Marshal.GetNativeVariantForObject(_selectedBaudString, vOut);
                else if (input != null)
                    if (int.TryParse((string) input, out _selectedBaud))
                    {
                        _selectedBaudString = (string) input;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(_selectedBaudString))
                        {
                            _selectedBaud = int.Parse(_selectedBaudString);
                        }
                        else
                        {
                            _selectedBaudString = "500000";
                            _selectedBaud = 500000;
                        }
                    }
                else
                    throw new ArgumentException(Resources.InOutParamCantBeNULL); // force an exception to be thrown
            }
            else
            {
                // We should never get here; EventArgs are required.
                throw new ArgumentException(Resources.EventArgsRequired); // force an exception to be thrown
            }
        }
    }
}