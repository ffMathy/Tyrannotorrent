using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Tyrannotorrent.Helpers;
using Tyrannotorrent.ViewModels;

namespace Tyrannotorrent
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {

            //TODO: instead of killing existing instances, it should transfer the download to the new instance.
            using (var currentProcess = Process.GetCurrentProcess())
            {
                foreach(var process in Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    if(process.Id != currentProcess.Id)
                    {
                        process.Kill();
                        Thread.Sleep(1000);
                    }
                }
            }

            //TODO: this registry stuff is really ugly. replace parts of it with a delegate method or something to make it shorter.
            using (var softwareKey = Registry.CurrentUser.OpenSubKey("Software", true))
            using (var classesKey = softwareKey.OpenSubKey("Classes", true))
            {
                var magnetKey = classesKey.OpenSubKey("Magnet", true);
                if (magnetKey == null)
                {
                    magnetKey = classesKey.CreateSubKey("Magnet");
                }

                using (magnetKey)
                {
                    magnetKey.SetValue(string.Empty, "Magnet URI");
                    magnetKey.SetValue("Content Type", "application/x-magnet");
                    magnetKey.SetValue("URL Protocol", string.Empty);

                    var shellKey = magnetKey.OpenSubKey("shell", true);
                    if (shellKey == null)
                    {
                        shellKey = magnetKey.CreateSubKey("shell");
                    }

                    using (shellKey)
                    {
                        shellKey.SetValue(string.Empty, "open");

                        var openKey = shellKey.OpenSubKey("open", true);
                        if (openKey == null)
                        {
                            openKey = shellKey.CreateSubKey("open");
                        }

                        using (openKey)
                        {
                            var commandKey = openKey.OpenSubKey("command", true);
                            if (commandKey == null)
                            {
                                commandKey = openKey.CreateSubKey("command");
                            }

                            using (commandKey)
                            using (var currentProcess = Process.GetCurrentProcess())
                            {
                                var executableName = currentProcess.ProcessName + ".exe";
                                var executablePath = Path.Combine(Environment.CurrentDirectory, executableName);
                                commandKey.SetValue(string.Empty, string.Format("\"{0}\" \"%1\"", executablePath));
                            }
                        }
                    }
                }

                var torrentKey = classesKey.OpenSubKey(".torrent", true);
                if (torrentKey == null)
                {
                    torrentKey = classesKey.CreateSubKey(".torrent");
                }

                using (torrentKey)
                {
                    torrentKey.SetValue(string.Empty, "Tyrannotorrent");
                    torrentKey.SetValue("URL Protocol", string.Empty);
                    torrentKey.SetValue("Content Type", "application/x-bittorrent");
                }

                var applicationKey = classesKey.OpenSubKey("Tyrannotorrent", true);
                if (applicationKey == null)
                {
                    applicationKey = classesKey.CreateSubKey("Tyrannotorrent");
                }

                using (applicationKey)
                {
                    var contentTypeKey = applicationKey.OpenSubKey("Content Type", true);
                    if (contentTypeKey == null)
                    {
                        contentTypeKey = applicationKey.CreateSubKey("Content Type");
                    }

                    using (contentTypeKey)
                    {
                        contentTypeKey.SetValue(string.Empty, "application/x-bittorrent");
                    }

                    var shellKey = applicationKey.OpenSubKey("shell", true);
                    if (shellKey == null)
                    {
                        shellKey = applicationKey.CreateSubKey("shell");
                    }

                    using (shellKey)
                    {
                        shellKey.SetValue(string.Empty, "open");

                        var openKey = shellKey.OpenSubKey("open", true);
                        if (openKey == null)
                        {
                            openKey = shellKey.CreateSubKey("open");
                        }

                        using (openKey)
                        {
                            var commandKey = openKey.OpenSubKey("command", true);
                            if (commandKey == null)
                            {
                                commandKey = openKey.CreateSubKey("command");
                            }

                            using (commandKey)
                            using (var currentProcess = Process.GetCurrentProcess())
                            {
                                var executableName = currentProcess.ProcessName + ".exe";
                                var executablePath = Path.Combine(Environment.CurrentDirectory, executableName);
                                commandKey.SetValue(string.Empty, string.Format("\"{0}\" \"%1\"", executablePath));
                            }
                        }
                    }
                }
            }

            var viewModel = MainWindowViewModel.Instance;

            //resume all torrents.
            var path = StorageHelper.TorrentsPath;
            foreach (var file in Directory.GetFiles(path, "*.torrent"))
            {
                viewModel.QueueTorrent(file);
            }

            //debugging mode? then load some sample data for testing.
            if (Debugger.IsAttached)
            {
                var desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                //add torrents from desktop.
                foreach (var file in Directory.GetFiles(desktopDirectory, "*.torrent"))
                {
                    viewModel.QueueTorrent(file);
                }

                //add magnet links from desktop.
                foreach (var file in Directory.GetFiles(desktopDirectory, "*.magnet"))
                {
                    viewModel.QueueTorrent(File.ReadAllText(file));
                }
            }

            var window = new MainWindow();

            //process new torrents.
            var arguments = e.Args;
            if(arguments.Length == 1)
            {
                var torrentFile = arguments[1];
                viewModel.QueueTorrent(torrentFile);
            } else if(arguments.Length == 2 && arguments[0] == "AUTO" && arguments[1] == "START")
            {
                window.WindowState = WindowState.Minimized;
            }

            window.Show();
        }
    }
}
