using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AVR.Debugger.Interfaces;
using WatchViewer.Properties;

namespace WatchViewer.Models
{
    internal class WatchDataContext : INotifyPropertyChanged
    {
        private readonly IDebuggerWrapper _debuggerWrapper;
        private bool _inDebug;

        public WatchDataContext(IDebuggerWrapper debuggerWrapper)
        {
            _debuggerWrapper = debuggerWrapper;
            Locals = new ObservableCollection<WatchVariable>();
        }

        public ObservableCollection<WatchVariable> Locals { get; set; }

        public bool InDebug
        {
            get { return _inDebug; }
            set
            {
                if (value == _inDebug) return;
                _inDebug = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Update()
        {
        }
    }
}