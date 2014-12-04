﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Tyranotorrent.ViewModels;

namespace Tyranotorrent
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                var viewModel = MainWindowViewModel.Instance;

                var desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                foreach (var file in Directory.GetFiles(desktopDirectory, "*.torrent"))
                {
                    viewModel.QueueTorrent(file);
                }
            }

            var window = new MainWindow();
            window.Show();
        }
    }
}
