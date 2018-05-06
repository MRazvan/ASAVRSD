using AVR.Debugger.Interfaces;

namespace AVR.Debugger.DeviceInfo
{
    [PluginInfo("Device Info", "1.0")]
    public class DeviceInfoPlugin : IPlugin
    {
        private DeviceReader _deviceReader = new DeviceReader();
        public void Initialize(Interfaces.IServiceProvider serviceProvider)
        {
            serviceProvider.AddService<IDeviceInfoProvider>(() => _deviceReader);
        }

        public void PostInitialize(){}
    }
}
