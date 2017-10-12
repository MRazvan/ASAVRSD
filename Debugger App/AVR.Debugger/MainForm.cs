using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AVR.Debugger.Interfaces;
using AVR.Debugger.Views;
using WeifenLuo.WinFormsUI.Docking;

namespace AVR.Debugger
{
    public partial class MainForm : Form, IDocumentHost, IServiceProvider
    {
        private DebuggerWrapper _debuggerWrapper;
        private DisassemblyView _disassemblyView;
        private readonly Dictionary<string, DockContent> _documents = new Dictionary<string, DockContent>();
        private SerialOutView _serialOut;

        public MainForm()
        {
            InitializeComponent();
        }

        public void OpenFile(string file)
        {
            if (!Path.IsPathRooted(file))
                file = Path.GetFullPath(file);
            var fileName = Path.GetFileName(file);
            var docKV = _documents.FirstOrDefault(d => d.Key == fileName);
            if (docKV.Value != null)
            {
                docKV.Value.Activate();
                docKV.Value.BringToFront();
            }
            else
            {
                var scv = new SourceCodeView();
                scv.LoadDataFromFile(file);
                AddDocument(fileName, scv);
                scv.Activate();
                scv.BringToFront();
            }
        }

        public void AddDocument(string key, DockContent content)
        {
            var docKV = _documents.FirstOrDefault(d => d.Key == key);
            if (docKV.Value != null)
            {
                docKV.Value.Activate();
            }
            else
            {
                if (dockPanel1.DocumentStyle == DocumentStyle.SystemMdi)
                {
                    content.MdiParent = this;
                    content.Show();
                }
                else
                {
                    content.Show(dockPanel1);
                }
                content.Closed += (sender, args) =>
                {
                    _documents.Remove(key);
                };
                _documents[key] = content;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _debuggerWrapper = new DebuggerWrapper(this);
            _debuggerWrapper.AddEventHandler(Interfaces.Events.DebugEnter, DebugEnter);
            _debuggerWrapper.AddEventHandler(Interfaces.Events.AfterDebugEnter, UpdateMenuItems);
            _debuggerWrapper.AddEventHandler(Interfaces.Events.Started, DebugStarted);
            _debuggerWrapper.AddEventHandler(Interfaces.Events.DebugLeave, UpdateMenuItems);
            _debuggerWrapper.AddUnknownDataHandler(UnknownDataHandler);
            UpdateMenuItems();
            RegisterService<IDebuggerWrapper>(() => _debuggerWrapper);
            RegisterService<IDocumentHost>(() => this);
            var pm = new PluginManager();
            pm.Initialize(this);
        }

        private void DebugStarted()
        {
            _serialOut = new SerialOutView();
            AddDocument("serialOut", _serialOut);
        }

        private void UnknownDataHandler(byte obj)
        {
            _serialOut.Append(Convert.ToChar(obj).ToString());
        }

        private void DebugEnter()
        {
            _disassemblyView.ScrollToPc((int) _debuggerWrapper.CpuState.PC);
            var lineInfo = _debuggerWrapper.GetLineFromAddr(_debuggerWrapper.CpuState.PC);
            if (lineInfo != null)
            {
                var sc = GetSCView(lineInfo.File);
                sc?.ScrollToLine(lineInfo.Line);
            }
        }

        private ISourceCodeView GetSCView(string lineInfoFile)
        {
            var dc = _documents.FirstOrDefault(d => d.Key == Path.GetFileName(lineInfoFile));
            var sc = dc.Value as ISourceCodeView;
            if (sc == null)
            {
                OpenFile(lineInfoFile);
                dc = _documents.FirstOrDefault(d => d.Key == Path.GetFileName(lineInfoFile));
            }
            return dc.Value as ISourceCodeView;
        }

        #region Main Menu Commands

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _debuggerWrapper.Load(openFileDialog.FileName);
                _disassemblyView = new DisassemblyView();
                _disassemblyView.LoadDisassembly(_debuggerWrapper.Disassembly);
                AddDocument("disassembly", _disassemblyView);
            }
        }

        #endregion

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _debuggerWrapper.Connect("COM4", 500000);
        }

        private void stepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _debuggerWrapper.Step();
        }

        private void continueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _debuggerWrapper.Continue();
        }

        private void UpdateMenuItems()
        {
            stepToolStripMenuItem.Enabled =
                continueToolStripMenuItem.Enabled =
                    _debuggerWrapper.InDebug;
        }

        private readonly Dictionary<Type, Func<object>> _services = new Dictionary<Type, Func<object>>();
        public void RegisterService<T>(Func<object> factoryOrInstance)
        {
            _services[typeof(T)] = factoryOrInstance;
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            return _services[serviceType]();
        }
    }
}