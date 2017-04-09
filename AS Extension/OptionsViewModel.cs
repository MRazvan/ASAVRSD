using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareDebuggerExtension
{
    public class OptionsViewModel : INotifyPropertyChanged
    {
        private string _arduinoPath;
        public string ArduinoPath {
            get { return _arduinoPath; }
            set {
                if (value != _arduinoPath)
                {
                    _arduinoPath = value;
                    OnPropertyChanged(nameof(ArduinoPath));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
