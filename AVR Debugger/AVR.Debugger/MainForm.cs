using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AVR.Debugger.Interfaces;
using AVR.Debugger.Views;
using WeifenLuo.WinFormsUI.Docking;
using AVR.Debugger.Interfaces.Models;

namespace AVR.Debugger
{
    public partial class MainForm : Form, IDocumentHost, Interfaces.IServiceProvider
    {
        private DebuggerWrapper _debuggerWrapper;
        private DisassemblyView _disassemblyView;
        private EventsService _eventsService;
        private PluginManager _pluginManager;
        private readonly Dictionary<string, DockContent> _documents = new Dictionary<string, DockContent>();
        private readonly Dictionary<Type, Func<object>> _services = new Dictionary<Type, Func<object>>();

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

        private void Activate(string file)
        {
            var view = _documents.FirstOrDefault(d => d.Key == file);
            view.Value?.BringToFront();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _eventsService = new EventsService(this);
            RegisterService<IDocumentHost>(() => this);
            RegisterService<IEventService>(() => _eventsService);
            _debuggerWrapper = new DebuggerWrapper(this);
            RegisterService<IDebuggerWrapper>(() => _debuggerWrapper);

            _eventsService.AddEventHandler(Interfaces.Events.Debug_Enter, (a) => DebugEnter());
            _eventsService.AddEventHandler(Interfaces.Events.Debug_AfterEnter, (a) => UpdateMenuItems());
            _eventsService.AddEventHandler(Interfaces.Events.Debug_Leave, (a) => UpdateMenuItems());
            UpdateMenuItems();

            _pluginManager = new PluginManager();
            _pluginManager.Initialize(this);

            // Load the device info
            var deviceService = this.GetService<IDeviceInfoProvider>();
            deviceService.LoadDevice(@"D:\Atmel\Studio\7.0\packs\atmel\ATmega_DFP\1.1.130\atdf\ATmega328.atdf");
        }
        private LineInfo lineInfo = null;
        private void DebugEnter()
        {
            _disassemblyView.ScrollToPc((int) _debuggerWrapper.CpuState.PC);
            if (lineInfo != null)
            {
                var sc = GetSCView(lineInfo.File);
                sc?.ClearMakers();
            }
            lineInfo = _debuggerWrapper.GetLineFromAddr(_debuggerWrapper.CpuState.PC);
            if (lineInfo != null)
            {
                var sc = GetSCView(lineInfo.File);

                sc?.ScrollToLine(lineInfo.Line);

                Activate(lineInfo.File);
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

        public void RegisterService<T>(Func<object> factoryOrInstance)
        {
            _services[typeof(T)] = factoryOrInstance;
        }

        public T GetService<T>()
        {
            if (!_services.ContainsKey(typeof(T)))
                return default(T);
            return (T)_services[typeof(T)]();
        }

        public void AddService<T>(object instance)
        {
            _services[typeof(T)] = () => instance;
        }

        public void AddService<T>(Func<object> factory)
        {
            _services[typeof(T)] = factory;
        }
    }
}