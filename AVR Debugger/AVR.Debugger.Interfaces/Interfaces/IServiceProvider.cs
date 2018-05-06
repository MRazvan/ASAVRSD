using System;

namespace AVR.Debugger.Interfaces
{
    public interface IServiceProvider
    {
        T GetService<T>();
        void AddService<T>(object instance);
        void AddService<T>(Func<object> factory);
    }
}
