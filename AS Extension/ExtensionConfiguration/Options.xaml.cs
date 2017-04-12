using System.Collections.Generic;
using System.Windows;

namespace SoftwareDebuggerExtension.ExtensionConfiguration
{
    /// <summary>
    ///     Interaction logic for Settings.xaml
    /// </summary>
    public partial class Options : Window
    {
        public Options()
        {
            InitializeComponent();
            DataContext = this;
        }

        private List<string> SolutionOptions => Settings.Instance.SolutionSettings.Options;
        public bool SolutionSettingsEnabled => Settings.Instance.SolutionSettingsLoaded;

        public bool VerboseLogging
        {
            get { return Settings.Instance.ExtensionSettings.VerboseLogging; }
            set { Settings.Instance.ExtensionSettings.VerboseLogging = value; }
        }

        public string ArduinoPath
        {
            get { return Settings.Instance.ExtensionSettings.ArduinoIdeLocation; }
            set { Settings.Instance.ExtensionSettings.ArduinoIdeLocation = value; }
        }

        public bool IsChecked_SingleStep
        {
            get { return SolutionOptions.Contains(Settings.DebuggingCapsStrings.SingleStep); }
            set { UpdateSolutionCaps(value, Settings.DebuggingCapsStrings.SingleStep); }
        }

        public bool IsChecked_EEPROM_Write
        {
            get { return SolutionOptions.Contains(Settings.DebuggingCapsStrings.EEPROMWrite); }
            set { UpdateSolutionCaps(value, Settings.DebuggingCapsStrings.EEPROMWrite); }
        }

        public bool IsChecked_EEPROM_Read
        {
            get { return SolutionOptions.Contains(Settings.DebuggingCapsStrings.EEPROMRead); }
            set { UpdateSolutionCaps(value, Settings.DebuggingCapsStrings.EEPROMRead); }
        }

        public bool IsChecked_WriteRam
        {
            get { return SolutionOptions.Contains(Settings.DebuggingCapsStrings.RamWrite); }
            set { UpdateSolutionCaps(value, Settings.DebuggingCapsStrings.RamWrite); }
        }

        public bool IsChecked_SaveContext
        {
            get { return SolutionOptions.Contains(Settings.DebuggingCapsStrings.SaveContext); }
            set { UpdateSolutionCaps(value, Settings.DebuggingCapsStrings.SaveContext); }
        }

        public bool IsChecked_DisableTimers
        {
            get { return SolutionOptions.Contains(Settings.DebuggingCapsStrings.DisableTimers); }
            set { UpdateSolutionCaps(value, Settings.DebuggingCapsStrings.DisableTimers); }
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UpdateSolutionCaps(bool enableCap, string cap)
        {
            if (enableCap == false)
                SolutionOptions.Remove(cap);
            else
                SolutionOptions.Add(cap);
        }
    }
}