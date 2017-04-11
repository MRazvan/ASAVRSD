using System;
using System.Collections.Generic;
using System.IO;
using EnvDTE;
using Newtonsoft.Json;

namespace SoftwareDebuggerExtension.ExtensionConfiguration
{
    public class Settings
    {
        public const string FileName = "config.json";
        public const string SolutionConfigName = "sd-config.json";

        public const string AVRDudePath = "hardware\\tools\\avr\\bin\\avrdude.exe";
        public const string AVRDudeConfig = "hardware\\tools\\avr\\etc\\avrdude.conf";

        public static List<string> DebuggingCaps = new List<string>
        {
            DebuggingCapsStrings.RamWrite,
            DebuggingCapsStrings.SaveContext,
            DebuggingCapsStrings.EEPROMRead,
            DebuggingCapsStrings.EEPROMWrite,
            DebuggingCapsStrings.SingleStep
        };

        static Settings()
        {
            Instance = new Settings();
        }

        private Settings()
        {
            SolutionSettings = new SolutionSettings();
            LoadOrCreateExtensionSettings();
            SolutionSettingsLoaded = false;
        }

        public static string AvrDudeFullPath => Path.Combine(Instance.ExtensionSettings.ArduinoIdeLocation, AVRDudePath)
        ;

        public static string AvrDudeConfigFullPath
            => Path.Combine(Instance.ExtensionSettings.ArduinoIdeLocation, AVRDudeConfig);

        public static Settings Instance { get; }
        public ExtensionSettings ExtensionSettings { get; private set; }
        public SolutionSettings SolutionSettings { get; private set; }
        public bool SolutionSettingsLoaded { get; set; }

        public void LoadExtensionSettings()
        {
            LoadOrCreateExtensionSettings();
        }

        public void SaveExtensionSettings()
        {
            var settingsFile = Path.Combine(PathHelper.GetExtensionAppDataFolder(), FileName);
            try
            {
                using (var sw = new StreamWriter(settingsFile))
                {
                    sw.Write(JsonConvert.SerializeObject(ExtensionSettings));
                }
            }
            catch (Exception ex)
            {
                VSTraceListener.Instance.LogException("Error saving extension settings", ex);
                ExtensionSettings = new ExtensionSettings {ArduinoIdeLocation = string.Empty};
            }
        }

        public void LoadSolutionSettings(Solution solution)
        {
            if (solution == null)
            {
                VSTraceListener.Instance.WriteLine("***Error -> Cannot load solution settings, the solution is null");
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(solution.FullName);
                var settingsFile = Path.Combine(directory, SolutionConfigName);
                if (File.Exists(settingsFile))
                    using (var sr = new StreamReader(settingsFile))
                    {
                        SolutionSettings = JsonConvert.DeserializeObject<SolutionSettings>(sr.ReadToEnd());
                    }
            }
            catch (Exception ex)
            {
                VSTraceListener.Instance.LogException("Error loading solution settings", ex);
                SolutionSettings = new SolutionSettings();
            }
        }

        public void SaveSolutionSettings(Solution solution)
        {
            if (solution == null)
            {
                VSTraceListener.Instance.WriteLine("***Error -> Cannot save solution settings, the solution is null");
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(solution.FullName);
                var settingsFile = Path.Combine(directory, SolutionConfigName);
                using (var sw = new StreamWriter(settingsFile))
                {
                    sw.Write(JsonConvert.SerializeObject(SolutionSettings));
                }
            }
            catch (Exception ex)
            {
                VSTraceListener.Instance.LogException("Error saving solution settings", ex);
            }
        }

        private void LoadOrCreateExtensionSettings()
        {
            var settingsFile = Path.Combine(PathHelper.GetExtensionAppDataFolder(), FileName);
            try
            {
                if (!File.Exists(settingsFile))
                {
                    ExtensionSettings = new ExtensionSettings {ArduinoIdeLocation = string.Empty};
                    using (var sw = new StreamWriter(settingsFile))
                    {
                        sw.Write(JsonConvert.SerializeObject(ExtensionSettings));
                    }
                }
                else
                {
                    using (var sr = new StreamReader(settingsFile))
                    {
                        ExtensionSettings = JsonConvert.DeserializeObject<ExtensionSettings>(sr.ReadToEnd());
                    }
                }
            }
            catch (Exception ex)
            {
                VSTraceListener.Instance.LogException("Error loading or creating extension settings", ex);
                ExtensionSettings = new ExtensionSettings {ArduinoIdeLocation = string.Empty};
            }
        }

        public class DebuggingCapsStrings
        {
            public const string RamWrite = "CAPS_RAM_WRITE";
            public const string SaveContext = "CAPS_SAVE_CTX";
            public const string EEPROMRead = "CAPS_EEPROM_READ";
            public const string EEPROMWrite = "CAPS_EEPROM_WRITE";
            public const string SingleStep = "CAPS_SINGLE_STEP";
        }
    }

    public class ExtensionSettings
    {
        public string ArduinoIdeLocation { get; set; } = string.Empty;
        public bool VerboseLogging { get; set; } = false;
    }

    public class SolutionSettings
    {
        public List<string> Options { get; set; } = new List<string>();
    }
}