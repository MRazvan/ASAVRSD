using System.Collections.Generic;

namespace AVR.Debugger.Interfaces
{
    public interface IDeviceInfoProvider
    {
        List<Device> Load(string devicePackPath);
        Device LoadDevice(string file);
        Device GetDevice(long id);
    }
}