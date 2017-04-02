using Atmel.Studio.Services;
using Atmel.VsIde.AvrStudio.Services.TargetService;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;

namespace SoftwareDebuggerExtension
{
    public class Commands
    {
        public delegate void RunAndAttachDelegate();
        public delegate void StepDelegate();
        public delegate void ContinueDelegate();

        private readonly IServiceProvider _serviceProvider;
        private string[] _ports;
        private string _selectedPort;

        private string[] _bauds = { "9600", "14400", "19200", "28800", "38400", "57600", "115200", "500000" };
        private string _selectedBaudString;
        private int _selectedBaud;
        private OleMenuCommand _stepCommand;
        private OleMenuCommand _continueCommand;

        public event RunAndAttachDelegate RunAndAttach;
        public event StepDelegate Step;
        public event ContinueDelegate Continue;

        public int BaudRate => _selectedBaud;
        public string Port => _selectedPort;

        #region CommandIds

        #endregion

        public Commands(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _selectedBaud = 500000;
            _selectedPort = SerialPort.GetPortNames().FirstOrDefault();
        }

        public void Initialize()
        {
            var mcs = _serviceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                mcs.AddCommand(new OleMenuCommand(AttachCallback, new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int)PkgCmdIDList.cmdAttach)));
                _stepCommand = new OleMenuCommand(StepCallback, new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int)PkgCmdIDList.cmdStep));
                mcs.AddCommand(_stepCommand);

                _continueCommand = new OleMenuCommand(ContinueCallback, new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int)PkgCmdIDList.cmdContinue));
                mcs.AddCommand(_continueCommand);

                mcs.AddCommand(new OleMenuCommand(OnPortDropDownCombo, new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int)PkgCmdIDList.cmdSelectPort)));
                mcs.AddCommand(new OleMenuCommand(OnPortDropDownComboList, new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int)PkgCmdIDList.cmdSelectPortList)));

                mcs.AddCommand(new OleMenuCommand(OnBaudDropDownCombo, new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int)PkgCmdIDList.cmdSelectBaud)));
                mcs.AddCommand(new OleMenuCommand(OnBaudDropDownComboList, new CommandID(GuidList.guidSoftwareDebuggerCmdSet, (int)PkgCmdIDList.cmdSelectBaudList)));
            }
            _continueCommand.Enabled = _stepCommand.Enabled = false;
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
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;
            _ports = SerialPort.GetPortNames();
            if (_selectedPort == null)
            {
                _selectedPort = _ports.FirstOrDefault();
            }
            if (eventArgs != null)
            {
                object inParam = eventArgs.InValue;
                IntPtr vOut = eventArgs.OutValue;

                if (inParam != null)
                {
                    throw (new ArgumentException(Resources.InParamIllegal)); // force an exception to be thrown
                }
                else if (vOut != IntPtr.Zero)
                {

                    Marshal.GetNativeVariantForObject(_ports, vOut);
                }
                else
                {
                    throw (new ArgumentException(Resources.OutParamRequired)); // force an exception to be thrown
                }
            }
        }
        private void OnPortDropDownCombo(object sender, EventArgs e)
        {
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null)
            {
                string newChoice = eventArgs.InValue as string;
                IntPtr vOut = eventArgs.OutValue;

                if (vOut != IntPtr.Zero)
                {
                    // when vOut is non-NULL, the IDE is requesting the current value for the combo
                    Marshal.GetNativeVariantForObject(_selectedPort, vOut);
                }

                else if (newChoice != null)
                {
                    // new value was selected or typed in
                    // see if it is one of our items
                    bool validInput = false;
                    int indexInput = -1;
                    var options = SerialPort.GetPortNames();
                    for (indexInput = 0; indexInput < _ports.Length; indexInput++)
                    {
                        if (string.Compare(_ports[indexInput], newChoice, StringComparison.CurrentCultureIgnoreCase) == 0)
                        {
                            validInput = true;
                            break;
                        }
                    }

                    if (validInput)
                    {
                        _selectedPort = _ports[indexInput];
                    }
                    else
                    {
                        throw (new ArgumentException(Resources.ParamNotValidStringInList)); // force an exception to be thrown
                    }
                }
            }
            else
            {
                // We should never get here; EventArgs are required.
                throw (new ArgumentException(Resources.EventArgsRequired)); // force an exception to be thrown
            }
        }

        private void OnBaudDropDownComboList(object sender, EventArgs e)
        {
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;
            if (_selectedBaudString == null)
            {
                _selectedBaudString = "500000";
                _selectedBaud = 500000;
            }
            if (eventArgs != null)
            {
                object inParam = eventArgs.InValue;
                IntPtr vOut = eventArgs.OutValue;

                if (inParam != null)
                {
                    throw (new ArgumentException(Resources.InParamIllegal)); // force an exception to be thrown
                }
                else if (vOut != IntPtr.Zero)
                {

                    Marshal.GetNativeVariantForObject(_bauds, vOut);
                }
                else
                {
                    throw (new ArgumentException(Resources.OutParamRequired)); // force an exception to be thrown
                }
            }
        }
        private void OnBaudDropDownCombo(object sender, EventArgs e)
        {
            if ((null == e) || (e == EventArgs.Empty))
            {
                // We should never get here; EventArgs are required.
                throw (new ArgumentException(Resources.EventArgsRequired)); // force an exception to be thrown
            }

            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null)
            {
                object input = eventArgs.InValue;
                IntPtr vOut = eventArgs.OutValue;

                if (vOut != IntPtr.Zero && input != null)
                {
                    throw (new ArgumentException(Resources.BothInOutParamsIllegal)); // force an exception to be thrown
                }
                else if (vOut != IntPtr.Zero)
                {
                    // when vOut is non-NULL, the IDE is requesting the current value for the combo
                    if (_selectedBaudString == null)
                    {
                        Marshal.GetNativeVariantForObject("500000", vOut);
                    }
                    else
                    {
                        Marshal.GetNativeVariantForObject(_selectedBaudString, vOut);
                    }

                }
                else if (input != null)
                {
                    // new zoom value was selected or typed in
                    if (int.TryParse((string)input, out _selectedBaud))
                    {
                        _selectedBaudString = (string)input;
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
                }
                else
                {
                    // We should never get here
                    throw (new ArgumentException(Resources.InOutParamCantBeNULL)); // force an exception to be thrown
                }
            }
            else
            {
                // We should never get here; EventArgs are required.
                throw (new ArgumentException(Resources.EventArgsRequired)); // force an exception to be thrown
            }
        }

    }
}
