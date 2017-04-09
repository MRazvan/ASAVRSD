using Atmel.Studio.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace SoftwareDebuggerExtension
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static List<string> sAvailableOptions = new List<string>
        {
            "CAPS_RAM_WRITE",
            "CAPS_SAVE_CTX",
            "CAPS_EEPROM_READ",
            "CAPS_EEPROM_WRITE",
            "CAPS_SINGLE_STEP"
        };

        private List<string> _projectDefines;
        private string _arduinoPath;

        public string ArduinoPath {
            get { return _arduinoPath; }
            set
            {
                if (value != _arduinoPath)
                {
                    _arduinoPath = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ArduinoPath)));
                }
            }
        }
        public bool IsChecked_SingleStep
        {
            get { return _projectDefines.Contains("CAPS_SINGLE_STEP"); }
            set
            {
                if (value == false)
                {
                    _projectDefines.Remove("CAPS_SINGLE_STEP");
                }
                else
                {
                    _projectDefines.Add("CAPS_SINGLE_STEP");
                }
            }
        }

        public bool IsChecked_EEPROM_Write
        {
            get { return _projectDefines.Contains("CAPS_EEPROM_WRITE"); }
            set
            {
                if (value == false)
                {
                    _projectDefines.Remove("CAPS_EEPROM_WRITE");
                }
                else
                {
                    _projectDefines.Add("CAPS_EEPROM_WRITE");
                }
            }
        }

        public bool IsChecked_EEPROM_Read
        {
            get { return _projectDefines.Contains("CAPS_EEPROM_READ"); }
            set
            {
                if (value == false)
                {
                    _projectDefines.Remove("CAPS_EEPROM_READ");
                }
                else
                {
                    _projectDefines.Add("CAPS_EEPROM_READ");
                }
            }
        }

        public bool IsChecked_WriteRam
        {
            get { return _projectDefines.Contains("CAPS_RAM_WRITE"); }
            set {
                if (value == false){
                    _projectDefines.Remove("CAPS_RAM_WRITE");
                } else {
                    _projectDefines.Add("CAPS_RAM_WRITE");
                }
            }
        }

        public bool IsChecked_SaveContext
        {
            get { return _projectDefines.Contains("CAPS_SAVE_CTX"); }
            set
            {
                if (value == false)
                {
                    _projectDefines.Remove("CAPS_SAVE_CTX");
                }
                else
                {
                    _projectDefines.Add("CAPS_SAVE_CTX");
                }
            }
        }

        public void SetArduinoPath(string arduinoIdePath)
        {
            ArduinoPath = arduinoIdePath;
        }

        public void SetProjectDefines(List<string> defines)
        {
            _projectDefines = defines;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("*"));
        }

        public Settings()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ArduinoPath = ATServiceProvider.DialogService.ShowEnhancedFolderBrowserDialog("Browse Arduino install location", ArduinoPath);
        }
    }
}
