using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareDebuggerExtension
{
    public static class PathHelper
    {
        public static string GetAppDataFolder(string folder)
        {
            var specificFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                folder);

            if (!Directory.Exists(specificFolder))
                Directory.CreateDirectory(specificFolder);
            return specificFolder;
        }

        public static string GetExtensionAppDataFolder()
        {
            return GetAppDataFolder("SoftwareDebugger");
        }
    }
}
