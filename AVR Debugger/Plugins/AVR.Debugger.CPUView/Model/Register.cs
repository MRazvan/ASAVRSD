using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CPUView.Annotations;

namespace AVR.Debugger.CPUView
{
    public class Register : INotifyPropertyChanged
    {
        private long _value;
        private bool _changed;
        public string RegisterName { get; set; }
        public byte Size { get; set; }

        public long Value
        {
            get { return _value; }
            set
            {
                if (value == _value)
                {
                    Changed = false;
                    return;
                }
                _value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayValue));
                Changed = true;
            }
        }

        public bool Changed
        {
            get { return _changed; }
            set
            {
                if (value == _changed) return;
                _changed = value;
                OnPropertyChanged();
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
                if (long.TryParse(val, NumberStyles.HexNumber, CultureInfo.InvariantCulture,  out temp))
                {
                    Value = temp;
                }
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