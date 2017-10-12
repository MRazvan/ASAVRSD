using System.Collections.Generic;
using System.IO;
using System.Linq;
using Atmel.Studio.Extensibility.Toolchain;
using Atmel.Studio.Framework;
using Atmel.Studio.Services;
using Atmel.Studio.Services.Device;
using EnvDTE;
using Atmel.VsIde.AvrStudio.Project.Management;
using System;

namespace SoftwareDebuggerExtension
{
    public class ProjectInfo
    {
        public ProjectInfo(Project proj)
        {
            IsValid = false;
            Project = proj;
            ProjectHandle = proj.Object as IProjectHandle;
            if (ProjectHandle == null)
                return;
            AvrNode = proj.Object as AvrProjectNode;
            Toolchain = AvrNode?.GetCurrentToolchain();
            Device = ATServiceProvider.DeviceService.GetDeviceFromName(ProjectHandle?.DeviceName);
            ToolName = proj.Properties.OfType<Property>().FirstOrDefault(p => p.Name == "ToolName")?.Value;
            var configurationManager = proj.ConfigurationManager;
            var activeConfiguration = configurationManager?.ActiveConfiguration;
            if (activeConfiguration != null)
            {
                var outputType =
                    activeConfiguration.Properties.OfType<Property>().FirstOrDefault(prop => prop.Name == "OutputType");
                IsExecutable = outputType?.Value == "Executable";

                if (IsExecutable)
                {
                    var outputFile = activeConfiguration.Properties.OfType<Property>().FirstOrDefault(prop => prop.Name == "OutputFile");
                    if (outputFile != null)
                    {
                        var outputDir = activeConfiguration.Properties.OfType<Property>().FirstOrDefault(prop => prop.Name == "OutputDirectory");
                        if (outputDir != null)
                        {
                            OutputPathHex = Path.Combine(outputDir.Value, $"{Path.GetFileNameWithoutExtension(outputFile.Value)}.hex");
                            OutputPathElf = Path.Combine(outputDir.Value, $"{Path.GetFileNameWithoutExtension(outputFile.Value)}.elf");
                        }
                    }
                }
            }
            var toolchain = proj.Properties.OfType<Property>().FirstOrDefault(prop => prop.Name == "ToolchainOptions");
            if (toolchain != null)
                ToolchainOptions = toolchain.Value as ProjectToolchainOptions;
            ToolchainPackageManager = ATServiceProvider.ToolchainService.GetPackagesManager(Toolchain?.Info.Name);
            IsValid = true;
        }

        public bool IsValid { get; set; }

        public bool IsExecutable { get; }
        public string OutputPathHex { get; private set; } = string.Empty;
        public string OutputPathElf { get; private set; } = string.Empty;
        public IProjectHandle ProjectHandle { get; }
        public ProjectToolchainOptions ToolchainOptions { get; }
        public IDevice Device { get; set; }

        public Project Project { get; set; }
        public string ToolName { get; set; }
        public AvrProjectNode AvrNode { get; set; }
        public IToolchain Toolchain { get; set; }
        public IToolchainPackagesManager ToolchainPackageManager { get; set; }

        public bool CanDebug()
        {
            return Device?.Architecture.ToLower() == "avr8" && ToolName == "Simulator" && IsExecutable;
        }

        public void UpdateToolchainOptions(List<string> options, List<string> debuggingCaps, List<string> includePaths)
        {
            var changed = AddIncludePaths(includePaths) || AddDefines(options, debuggingCaps);
            if (changed)
            {
                ProjectHandle?.GetConfigNames()
                    .Where(config => config.ToLower().Contains("debug"))
                    .ForEach(config =>
                    {
                        ProjectHandle?.SetProperty("ToolchainSettings", ToolchainOptions.ToString(), config);
                    });
            }
        }

        private bool AddIncludePaths(List<string> addPaths)
        {
            var changed = false;
            var includePaths = ToolchainOptions.CCompiler?.IncludePaths;
            if (includePaths != null)
            {
                foreach (var path in addPaths)
                {
                    if (!includePaths.Contains(path))
                    {
                        changed = true;
                        includePaths.Add(path);
                    }
                }
            }

            includePaths = ToolchainOptions.CppCompiler?.IncludePaths;
            if (includePaths != null)
            {
                foreach (var path in addPaths)
                {
                    if (!includePaths.Contains(path))
                    {
                        changed = true;
                        includePaths.Add(path);
                    }
                }
            }
            return changed;
        }

        private bool AddDefines(List<string> defines, List<string> allDefines)
        {
            var changed = false;

            if (ToolchainOptions.CCompiler != null)
                changed |= UpdateDefines(ToolchainOptions.CCompiler.SymbolDefines, defines, allDefines);

            if (ToolchainOptions.CppCompiler != null)
                changed |= UpdateDefines(ToolchainOptions.CppCompiler.SymbolDefines, defines, allDefines);
            return changed;
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