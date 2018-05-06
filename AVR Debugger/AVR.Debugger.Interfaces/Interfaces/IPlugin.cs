using System;

namespace AVR.Debugger.Interfaces
{
    public interface IPlugin
    {
        void Initialize(IServiceProvider serviceProvider);
        void PostInitialize();
    }
}
