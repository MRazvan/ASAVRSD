using System;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using WeifenLuo.WinFormsUI.Docking;
using System.Collections.Generic;
using System.Diagnostics;

namespace AVR.Debugger {
	public partial class MainForm : Form, IDocumentHost
    {
        private DebuggerWrapper _debuggerWrapper;
        private DisassemblyView _disassemblyView;
        private SerialOutView _serialOut;
        private CPUInfoView _cpuInfo;
        private Dictionary<string, DockContent> _documents = new Dictionary<string, DockContent>();


        public MainForm() {
			InitializeComponent();
		}

		private void MainForm_Load(object sender, EventArgs e) {
            _debuggerWrapper = new DebuggerWrapper(this);
            _debuggerWrapper.AddEventHandler(Debugger.Events.DebugEnter, DebugEnter);
            _debuggerWrapper.AddEventHandler(Debugger.Events.AfterDebugEnter, UpdateMenuItems);
            _debuggerWrapper.AddEventHandler(Debugger.Events.Started, DebugStarted);
            _debuggerWrapper.AddEventHandler(Debugger.Events.DebugLeave, UpdateMenuItems);
            _debuggerWrapper.AddUnknownDataHandler(UnknownDataHandler);
            UpdateMenuItems();
        }

        private void DebugStarted()
        {
            _serialOut = new SerialOutView();
            AddDocument("serialOut", _serialOut);
            _cpuInfo = new CPUInfoView();
            AddDocument("cpuInfo", _cpuInfo);
        }

        private void UnknownDataHandler(byte obj)
        {
            _serialOut.Append(Convert.ToChar(obj).ToString());
        }

        private void DebugEnter()
        {
            _disassemblyView.ScrollToPc((int)_debuggerWrapper.CpuState.PC);
            _cpuInfo.SetCpuInfo(_debuggerWrapper.CpuState);
            var lineInfo = _debuggerWrapper.GetLineFromAddr(_debuggerWrapper.CpuState.PC);
            if (lineInfo != null)
            {
                var dc = _documents.FirstOrDefault(d => d.Key == Path.GetFileName(lineInfo.File));
                var sc = dc.Value as ISourceCodeView;
                sc?.ScrollToLine(lineInfo.Line);
            }
        }

        #region Main Menu Commands

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _debuggerWrapper.Load(openFileDialog.FileName);
                _disassemblyView = new DisassemblyView();
                _disassemblyView.LoadDisassembly(_debuggerWrapper.Disassembly);
                AddDocument("disassembly", _disassemblyView);
                _debuggerWrapper.Symbols
                    .Where(s => !string.IsNullOrWhiteSpace(s.File))
                    .Select(s => Path.GetFullPath(s.File))
                    .Where(s => File.Exists(s))
                    .Distinct()
                    .ToList()
                    .ForEach(OpenFile);
            }
        }

		#endregion

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _debuggerWrapper.Connect("COM3", 500000);
        }

        private void stepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _debuggerWrapper.Step();
        }

        private void continueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _debuggerWrapper.Step();
        }

        private void UpdateMenuItems(bool enabled)
        {
            Debug.WriteLine("Update menu items " + _debuggerWrapper.InDebug);
            stepToolStripMenuItem.Enabled =
                continueToolStripMenuItem.Enabled = 
                _debuggerWrapper.InDebug;
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
            }
            else
            {
                var scv = new SourceCodeView(dockPanel1);
                scv.LoadDataFromFile(file);
                _documents[fileName] = scv;
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
                _documents[key] = content;
            }
        }
    }
}
