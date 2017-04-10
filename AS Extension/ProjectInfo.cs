using System.Collections.Generic;
using System.IO;
using System.Linq;
using Atmel.Studio.Extensibility.Toolchain;
using Atmel.Studio.Services;
using Atmel.Studio.Services.Device;
using EnvDTE;

namespace SoftwareDebuggerExtension
{
    public class ProjectInfo
    {
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
                var outputType =
                    activeConfiguration.Properties.OfType<Property>().FirstOrDefault(prop => prop.Name == "OutputType");
                IsExecutable = outputType?.Value == "Executable";

                var outputFile =
                    activeConfiguration.Properties.OfType<Property>().FirstOrDefault(prop => prop.Name == "OutputFile");
                if (outputFile != null)
                {
                    var outputDir =
                        activeConfiguration.Properties.OfType<Property>()
                            .FirstOrDefault(prop => prop.Name == "OutputDirectory");
                    if (outputDir != null)
                        OutputPath = Path.Combine(outputDir.Value,
                            $"{Path.GetFileNameWithoutExtension(outputFile.Value)}.hex");
                }
            }
            var toolchain = proj.Properties.OfType<Property>().FirstOrDefault(prop => prop.Name == "ToolchainOptions");
            if (toolchain != null)
                ToolchainOptions = toolchain.Value as ProjectToolchainOptions;
        }

        public bool IsExecutable { get; }
        public string OutputPath { get; private set; } = string.Empty;
        public IProjectHandle ProjectHandle { get; }
        public ProjectToolchainOptions ToolchainOptions { get; }
        public IDevice Device { get; set; }
        public Project Project { get; set; }
        public string ToolName { get; set; }

        public bool CanDebug()
        {
            return Device?.Architecture.ToLower() == "avr8" && ToolName == "Simulator" && IsExecutable;
        }

        public void AddDefines(List<string> defines, List<string> allDefines)
        {
            var changed = false;

            if (ToolchainOptions.CCompiler != null)
                changed |= UpdateDefines(ToolchainOptions.CCompiler.SymbolDefines, defines, allDefines);

            if (ToolchainOptions.CppCompiler != null)
                changed |= UpdateDefines(ToolchainOptions.CppCompiler.SymbolDefines, defines, allDefines);

            if (changed)
                ProjectHandle?.SetPropertyForAllConfiguration("ToolchainSettings", ToolchainOptions.ToString());
        }

        private bool UpdateDefines(IList<string> destination, IList<string> source, IList<string> allcustom)
        {
            var changed = false;
            var removedDefines = allcustom.Where(c => !source.Contains(c)).ToList();
            foreach (var define in source)
                if (!destination.Contains(define))
                {
                    changed = true;
                    destination.Add(define);
                }
            foreach (var define in removedDefines)
                while (destination.Contains(define))
                {
                    destination.Remove(define);
                    changed = true;
                }
            var tmp = destination.Distinct().ToList();
            destination.Clear();
            tmp.ForEach(destination.Add);
            return changed;
        }
    }
}