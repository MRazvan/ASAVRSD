using Atmel.Studio.Extensibility.Toolchain;
using Atmel.Studio.Services;
using Atmel.Studio.Services.Device;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareDebuggerExtension
{
    public class ProjectInfo
    {
        public bool IsExecutable { get; private set; } = false;
        public string OutputPath { get; private set; } = string.Empty;
        public IProjectHandle ProjectHandle { get; private set; } = null;
        public ProjectToolchainOptions ToolchainOptions { get; private set; } = null;
        public IDevice Device { get; set; } = null;
        public Project Project { get; set; } = null;
        public string ToolName { get; set; } = string.Empty;
        public ProjectInfo(Project proj)
        {
            Project = proj;
            ProjectHandle = proj.Object as IProjectHandle;
            Device = ATServiceProvider.DeviceService.GetDeviceFromName(ProjectHandle?.DeviceName);
            ToolName = proj.Properties.OfType<Property>().FirstOrDefault(p => p.Name == "ToolName")?.Value;
            var configurationManager = proj.ConfigurationManager;
            var activeConfiguration = configurationManager?.ActiveConfiguration;
            if (activeConfiguration != null)
            {
                var props = activeConfiguration.Properties.OfType<Property>().Select(n => new { n.Name, n.Value }).ToList();
                var outputType = activeConfiguration.Properties.OfType<Property>().FirstOrDefault(prop => prop.Name == "OutputType");
                IsExecutable = outputType?.Value == "Executable";

                var outputFile = activeConfiguration.Properties.OfType<Property>().FirstOrDefault(prop => prop.Name == "OutputFile");
                if (outputFile != null)
                {
                    var outputDir = activeConfiguration.Properties.OfType<Property>().FirstOrDefault(prop => prop.Name == "OutputDirectory");
                    if (outputDir != null)
                    {
                        OutputPath = Path.Combine(outputDir.Value, $"{Path.GetFileNameWithoutExtension(outputFile.Value)}.hex");
                    }
                }
            }
            var toolchain = proj.Properties.OfType<Property>().FirstOrDefault(prop => prop.Name == "ToolchainOptions");
            if (toolchain != null)
            {
                ToolchainOptions = toolchain.Value as ProjectToolchainOptions; 
            }
        }

        public bool CanDebug()
        {
            return Device?.Architecture.ToLower() == "avr8" && ToolName == "Simulator" && IsExecutable;
        }

        public void AddDefines(List<string> defines, List<string> allDefines)
        {
            var changedCDefines = UpdateDefines(ToolchainOptions.CCompiler.SymbolDefines, defines, allDefines);
            var changedCPPDefines = UpdateDefines(ToolchainOptions.CppCompiler.SymbolDefines, defines, allDefines);
            if (changedCDefines || changedCPPDefines)
            {
                ProjectHandle?.SetPropertyForAllConfiguration("ToolchainSettings", ToolchainOptions.ToString());
            }
        }

        private bool UpdateDefines(IList<string> destination, IList<string> source, IList<string> allcustom)
        {
            bool changed = false;
            var removedDefines = allcustom.Intersect(source);
            foreach(var define in source)
            {
                if (!destination.Contains(define))
                {
                    changed = true;
                    destination.Add(define);
                }
            }
            foreach(var define in removedDefines)
            {
                if (destination.Contains(define))
                {
                    destination.Remove(define);
                    changed = true;
                }
            }
            return changed;
        }
    }
}
