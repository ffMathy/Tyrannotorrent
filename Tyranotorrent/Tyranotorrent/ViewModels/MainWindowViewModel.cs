using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;
using MonoTorrent.Dht;
using MonoTorrent.Dht.Listeners;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Tyranotorrent.MonoTorrent;

namespace Tyranotorrent.ViewModels
{
    class MainWindowViewModel
    {

        public ObservableCollection<TorrentManagerViewModelFacade> Downloads { get; private set; }

        private readonly TorrentSettings torrentSettings;
        private readonly ClientEngine clientEngine;

        private MainWindowViewModel()
        {
            Downloads = new ObservableCollection<TorrentManagerViewModelFacade>();

            const int port = 25555;

            var engineSettings = new EngineSettings(Environment.CurrentDirectory, port);
            engineSettings.PreferEncryption = false;
            engineSettings.AllowedEncryption = EncryptionTypes.All;

            torrentSettings = new TorrentSettings(4, 150, 0, 0);

            clientEngine = new ClientEngine(engineSettings);
            clientEngine.CriticalException += ClientEngine_CriticalException;

            clientEngine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, port));

            var listener = new DhtListener(new IPEndPoint(IPAddress.Any, port));
            var nodeEngine = new DhtEngine(listener);

            clientEngine.RegisterDht(nodeEngine);

            listener.Start();
            nodeEngine.Start();
        }

        private void ClientEngine_CriticalException(object sender, CriticalExceptionEventArgs e)
        {
            MessageBox.Show("Woops. A critical error occured." + Environment.NewLine + Environment.NewLine + e.Exception, "Woops!", MessageBoxButton.OK, MessageBoxImage.Error);
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
        /// <param name="torrentPath">The Magnet link or Torrent file location.</param>
        public void QueueTorrent(string torrentPath)
        {
            TorrentManager manager;

            Torrent torrent;
            if (Torrent.TryLoad(torrentPath, out torrent))
            {
                var savePath = Path.Combine(Environment.CurrentDirectory, torrent.Name);
                manager = new TorrentManager(torrent, savePath, torrentSettings);
            } else if(torrentPath.StartsWith("magnet:"))
            {
                var link = new MagnetLink(torrentPath);
                var savePath = Path.Combine(Environment.CurrentDirectory, link.Name);
                manager = new TorrentManager(link, savePath, torrentSettings, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Temporary.torrent"));
            } else
            {
                MessageBox.Show("Unrecognized torrent format.", "Sorry about that", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            clientEngine.Register(manager);

            var viewModel = new TorrentManagerViewModelFacade(manager);
            viewModel.TorrentDownloaded += ViewModel_TorrentDownloaded;

            Downloads.Add(viewModel);

            manager.Start();
        }

        private void ViewModel_TorrentDownloaded(TorrentManagerViewModelFacade torrent)
        {
            Downloads.Remove(torrent);
            torrent.Dispose();
        }
    }
}
