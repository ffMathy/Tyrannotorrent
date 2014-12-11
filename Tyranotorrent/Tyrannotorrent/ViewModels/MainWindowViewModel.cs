using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Tyrannotorrent.Facades;
using Tyrannotorrent.Factories;
using System.Diagnostics;
using Tyrannotorrent.Helpers;
using Ragnar;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using Tyrannotorrent.Commands;
using System.Windows.Input;

namespace Tyrannotorrent.ViewModels
{
    class MainWindowViewModel : IDisposable, INotifyPropertyChanged
    {

        private readonly Session torrentSession;

        public ObservableCollection<TorrentHandleViewModelFacade> Downloads { get; private set; }

        public bool ShutDownPC { get; set; }

        private MainWindowViewModel()
        {
            Downloads = new ObservableCollection<TorrentHandleViewModelFacade>();

            torrentSession = new Session();

            StopTorrentCommand = new RelayCommand((arg) => {
                var torrent = (TorrentHandleViewModelFacade)arg;
                torrent.StopButtonVisibility = Visibility.Collapsed;
                torrent.StartButtonVisibility = Visibility.Visible;

                var handle = torrent.TorrentHandle;
                handle.Pause();

                torrentSession.RemoveTorrent(handle);
            });
            StartTorrentCommand = new RelayCommand((arg) =>
            {
                var torrent = (TorrentHandleViewModelFacade)arg;
                torrent.StartButtonVisibility = Visibility.Collapsed;
                torrent.StopButtonVisibility = Visibility.Visible;

                var handle = torrent.TorrentHandle;
                handle.Resume();

                torrentSession.AddTorrent(new AddTorrentParams()
                {
                    TorrentInfo = handle.TorrentFile
                });
            });
            RemoveTorrentCommand = new RelayCommand((arg) =>
            {
                var torrent = (TorrentHandleViewModelFacade)arg;
                RemoveTorrent(torrent);
            });

            Load();

        }

        public ICommand StopTorrentCommand
        {
            get; private set;
        }

        public ICommand StartTorrentCommand
        {
            get; private set;
        }

        public ICommand RemoveTorrentCommand
        {
            get; private set;
        }

        public double Progress
        {
            get
            {
                return Downloads.Average(d => d.Progress) / 100.0;
            }
        }

        public string Description
        {
            get
            {
                return string.Format("Downloading {0} " + (Downloads.Count > 0 ? "torrents" : "torrent"), Downloads.Count);
            }
        }

        private async void Load()
        {

            var nodesFilePath = Path.Combine(PathHelper.TorrentsPath, "SessionState");
            if (File.Exists(nodesFilePath))
            {
                var sessionState = File.ReadAllBytes(nodesFilePath);
                torrentSession.LoadState(sessionState);
            }

            const int portDiversity = 100;
            const int port = 25555;

            var listening = false;
            while (!listening)
            {
                try
                {
                    torrentSession.ListenOn(port - portDiversity / 2, port + portDiversity / 2);
                    listening = true;
                }
                catch
                {
                    //wait for firewall warning to disappear asynchronously.
                    await Task.Delay(1000);
                }
            }

            //let's do some funky routing.
            torrentSession.StartNatPmp();
            torrentSession.StartUpnp();
            torrentSession.StartDht();
            torrentSession.StartLsd();
        }

        private static MainWindowViewModel instance;
        public static MainWindowViewModel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MainWindowViewModel();
                }
                return instance;
            }
        }

        /// <summary>
        /// Queues a Torrent or Magnet link for download.
        /// </summary>
        /// <param name="input">The Magnet link or Torrent file location.</param>
        public async void QueueTorrent(string input)
        {
            TorrentManagerFactory factory;

            if (input.StartsWith("magnet:"))
            {
                factory = new MagnetTorrentManagerFactory();
            }
            else if (File.Exists(input) && string.Equals(Path.GetExtension(input), ".torrent", StringComparison.OrdinalIgnoreCase))
            {
                factory = new TorrentFileTorrentManagerFactory();
            }
            else
            {
                MessageBox.Show("Unrecognized torrent format.", "Sorry about that", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var torrentHandle = await factory.CreateTorrent(torrentSession, input);

            var viewModel = new TorrentHandleViewModelFacade(torrentHandle);
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.TorrentDownloaded += ViewModel_TorrentDownloaded;

            Downloads.Add(viewModel);

            StartupHelper.SetStartNextTimeWithWindows(true);
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }

            NotifyPropertyChanged("Description");
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void ViewModel_TorrentDownloaded(TorrentHandleViewModelFacade torrent)
        {
            RemoveTorrent(torrent);

            if (Downloads.Count == 0)
            {
                StartupHelper.SetStartNextTimeWithWindows(false);

                if (ShutDownPC)
                {
#pragma warning disable CS0642
                    using (Process.Start("shutdown", "/s /t 0")) ;
#pragma warning restore CS0642
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }

        }

        private void RemoveTorrent(TorrentHandleViewModelFacade torrent)
        {
            var torrentFilePath = torrent.TorrentFilePath;
            if (File.Exists(torrentFilePath))
            {
                File.Delete(torrentFilePath);
            }

            var torrentDownloadPath = torrent.TorrentSavePath;
            Process.Start(torrentDownloadPath);

            torrentSession.RemoveTorrent(torrent.TorrentHandle);
            Downloads.Remove(torrent);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void Dispose(bool disposing)
        {
            var nodesFilePath = Path.Combine(PathHelper.TorrentsPath, "SessionState");
            var data = torrentSession.SaveState();
            File.WriteAllBytes(nodesFilePath, data);

            if (!disposedValue)
            {
                if (disposing)
                {
                    Downloads.Clear();
                    foreach (var torrent in Downloads)
                    {
                        torrentSession.RemoveTorrent(torrent.TorrentHandle);
                        torrent.Dispose();
                    }

                    torrentSession.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
