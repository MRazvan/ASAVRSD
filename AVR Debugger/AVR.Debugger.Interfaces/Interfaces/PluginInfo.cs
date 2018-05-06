using System;

namespace AVR.Debugger.Interfaces
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PluginInfoAttribute : Attribute
    {
        public string Name { get; private set; }
        public string Version { get; private set; }

        public PluginInfoAttribute(string name, string version)
        {
            Name = name;
            Version = version;
        }
    }
}
