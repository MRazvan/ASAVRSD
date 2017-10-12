using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AVR.Debugger.Interfaces;

namespace AVR.Debugger
{
    class PluginManager
    {
        private List<IPlugin> _plugins;
        public PluginManager()
        {
            _plugins = new List<IPlugin>();
            var pluginType = typeof(IPlugin);
            var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var pluginsPath = Path.Combine(folder, "plugins");
            foreach (var file in Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    var assembly = Assembly.LoadFile(file);
                    var plugins = assembly.GetTypes().Where(t => pluginType.IsAssignableFrom(t));
                    if (plugins.Any())
                    {
                        foreach (var plugin in plugins)
                        {
                            try
                            {
                                var instance = Activator.CreateInstance(plugin);
                                _plugins.Add(instance as IPlugin);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                    }
                        
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            _plugins.ForEach(p => p.Initialize(serviceProvider));
        }
    }
}
