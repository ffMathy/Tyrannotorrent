using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tyrannotorrent.Helpers
{
    static class StartupHelper
    {
        public static void SetStartNextTimeWithWindows(bool startWithWindows)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce", true))
            {
                if (startWithWindows)
                {
                    var values = key.GetValueNames();
                    if (values.Contains("Tyrannotorrent"))
                    {
                        key.DeleteValue("Tyrannotorrent");
                    }
                }
                else
                {
                    using (var currentProcess = Process.GetCurrentProcess())
                    {
                        var executableName = currentProcess.ProcessName + ".exe";
                        var executablePath = Path.Combine(PathHelper.CurrentExecutablePath);
                        key.SetValue("Tyrannotorrent", string.Format("\"{0}\" AUTO START", executablePath));
                    }
                }
            }
        }
    }
}
