using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using WatchViewer.Properties;

namespace AVR.Debugger.WatchViewer.Models
{
    internal class WatchVariable : INotifyPropertyChanged
    {
        private long _address;
        private bool _hasChildren;
        private bool _isExpanded;
        private bool _isReadonly;
        private ObservableCollection<WatchVariable> _locals;
        private string _name;
        private string _parent;
        private int _size;
        private string _type;
        private long _value;

        public WatchVariable()
        {
            Locals = new ObservableCollection<WatchVariable>();
        }

        public ObservableCollection<WatchVariable> Locals
        {
            get { return _locals; }
            set
            {
                if (Equals(value, _locals)) return;
                _locals = value;
                OnPropertyChanged();
            }
        }

        public bool IsReadonly
        {
            get { return _isReadonly; }
            set
            {
                if (value == _isReadonly) return;
                _isReadonly = value;
                OnPropertyChanged();
            }
        }

        public bool HasChildren
        {
            get { return _hasChildren; }
            set
            {
                if (value == _hasChildren) return;
                _hasChildren = value;
                OnPropertyChanged();
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value == _isExpanded) return;
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public string Parent
        {
            get { return _parent; }
            set
            {
                if (value == _parent) return;
                _parent = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Type
        {
            get { return _type; }
            set
            {
                if (value == _type) return;
                _type = value;
                OnPropertyChanged();
            }
        }

        public string DisplayAddress => "0x"+Address.ToString("X4");

        public long Address
        {
            get { return _address; }
            set
            {
                if (value == _address) return;
                _address = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayAddress));
            }
        }

        public int Size
        {
            get { return _size; }
            set
            {
                if (value == _size) return;
                _size = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayValue));
            }
        }

        public long Value
        {
            get { return _value; }
            set
            {
                if (value == _value) return;
                _value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayValue));
            }
        }

        public string DisplayValue
        {
            get
            {
                var s = Size / 4;
                return $"0x{Value.ToString($"X{s}")}";
            }
            set
            {
                var val = value.TrimStart('0', 'x');
                if (string.IsNullOrEmpty(val))
                    val = "0";
                long temp = 0;
                if (long.TryParse(val, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out temp))
                    Value = temp;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}