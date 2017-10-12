using System;

namespace AVR.Debugger.Interfaces
{
    public interface IPlugin
    {
        string Name { get; }
        void Initialize(IServiceProvider serviceProvider);
    }
}
