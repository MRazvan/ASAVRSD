using System;
using System.Drawing;
using System.Windows.Forms;
using Debugger.Server.Transports;
using Debugger.Server;
using Debugger.Server.Commands;
using System.IO;
using System.Linq;
using WeifenLuo.WinFormsUI.Docking;
using System.Collections.Generic;

namespace AVR.Debugger {
	public partial class MainForm : Form, IDocumentHost
    {
		public MainForm() {
			InitializeComponent();
		}

        DisassemblyView _disassemblyView;

		private void MainForm_Load(object sender, EventArgs e) {
            _disassemblyView = new DisassemblyView(dockPanel1);
		}

        #region Main Menu Commands

        public static String GetAbsolutePath(String relativePath, String basePath)
        {
            if (relativePath == null)
                return null;
            if (basePath == null)
                basePath = Path.GetFullPath("."); // quick way of getting current working directory
            else
                basePath = GetAbsolutePath(basePath, null); // to be REALLY sure ;)
            String path;
            // specific for windows paths starting on \ - they need the drive added to them.
            // I constructed this piece like this for possible Mono support.
            if (!Path.IsPathRooted(relativePath) || "\\".Equals(Path.GetPathRoot(relativePath)))
            {
                if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
                    path = Path.Combine(Path.GetPathRoot(basePath), relativePath.TrimStart(Path.DirectorySeparatorChar));
                else
                    path = Path.Combine(basePath, relativePath);
            }
            else
                path = relativePath;
            // resolves any internal "..\" to get the true full path.
            return Path.GetFullPath(path);
        }

        SourceCodeView _mainCodeView;

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _elfFile = openFileDialog.FileName;

                _disassemblyView.OpenFile(openFileDialog.FileName);
                var symbols = (new AvrNm().GetInfo(openFileDialog.FileName));
                symbols
                    .Where(s => !string.IsNullOrWhiteSpace(s.File))
                    .Where(s => File.Exists(s))
                    .Select(s => GetAbsolutePath(s.File, null))
                    .Distinct()
                    .ForEach(f => OpenFile(f));
            }
        }

		private void zoomInToolStripMenuItem_Click(object sender, EventArgs e) {
			_disassemblyView.ZoomIn();
		}

		private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e) {
            _disassemblyView.ZoomOut();
		}

		private void zoom100ToolStripMenuItem_Click(object sender, EventArgs e) {
            _disassemblyView.ZoomDefault();
		}
		
		#endregion

		#region Utils

		public static Color IntToColor(int rgb) {
			return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
		}

		public void InvokeIfNeeded(Action action) {
			if (this.InvokeRequired) {
				this.BeginInvoke(action);
			} else {
				action.Invoke();
			}
		}

        #endregion

        DebugServer _srv;

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _srv = new DebugServer();
            var transport = new SerialTransport();
            transport.SetPort("COM3");
            transport.SetSpeed(500000);

            _srv.SetTransport(transport);

            _srv.DebuggerAttached += _srv_DebuggerAttached;
            _srv.Start();
            _srv.ResetTarget();

            _serialOut = new SerialOutView(dockPanel1);
            _srv.UnknownData += _srv_UnknownData;
            _cpuOut = new CPUInfoView(dockPanel1);
        }

        private void _srv_UnknownData(byte data)
        {
            InvokeIfNeeded(() =>
            {
                _serialOut.Append(Convert.ToChar(data).ToString());
            });
        }

        private byte[] WaitForData(IDebugCommand cmd)
        {
            var cmdTask = _srv.AddCommand(cmd);
            cmdTask.Wait();
            return cmdTask.Result;
        }

        private void _srv_DebuggerAttached()
        {
            InvokeIfNeeded(() => { DebuggerAttached(); });
        }

        private AvrAddrToLine _addr2Line = new AvrAddrToLine();
        private string _elfFile;
        private SerialOutView _serialOut;

        private void DebuggerAttached()
        {
            var dbgCxtLocationData = WaitForData(new DebugCommand_CtxRead());

            var location = (dbgCxtLocationData[0] << 8) | (dbgCxtLocationData[1]);
            var size = (dbgCxtLocationData[2] << 8) | (dbgCxtLocationData[3]);

            var ramdData = WaitForData(new DebugCommand_Ram_Read((uint)location, (uint)size));
            var cpuState = GetCpuState(ramdData);
            _cpuOut.SetCpuInfo(cpuState);
            _disassemblyView.ScrollToPc((int)cpuState.PC * 2);
            var li = _addr2Line.GetLineInfo((uint)(cpuState.PC * 2), _elfFile);
            if (li != null)
            {
                var dc = _documents.FirstOrDefault(d => d.Key == Path.GetFileName(li.File));
                var sc = dc.Value as ISourceCodeView;
                sc?.ScrollToLine(li.Line);
            }

            stepToolStripMenuItem.Enabled = true;
        }

        byte[] registersBuffer = new byte[32];
        CpuState _state = new CpuState();
        private CPUInfoView _cpuOut;

        private CpuState GetCpuState(byte[] ramdData)
        {
            Array.Copy(ramdData, 0, registersBuffer, 0, 32);
            _state.Registers = registersBuffer;
            _state.PC = (uint)((ramdData[35] << 8) | (ramdData[34]));
            _state.Stack = (uint)((ramdData[33] << 8) | (ramdData[32]));
            return _state;
        }

        private void stepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stepToolStripMenuItem.Enabled = false;
            _srv.Step();
        }

        private void continueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _srv.Continue();
        }

        private Dictionary<string, DockContent> _documents = new Dictionary<string, DockContent>();
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
