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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Tyranotorrent.Facades;
using System.Collections.Generic;
using Tyranotorrent.Factories;

namespace Tyranotorrent.ViewModels
{
    class MainWindowViewModel
    {

        public ObservableCollection<TorrentManagerViewModelFacade> Downloads { get; private set; }

        private readonly ClientEngine clientEngine;

        private MainWindowViewModel()
        {
            Downloads = new ObservableCollection<TorrentManagerViewModelFacade>();

            var random = new Random((int)DateTime.UtcNow.Ticks);
            var port = 25555 + random.Next(-100, 100);

            var engineSettings = new EngineSettings(Environment.CurrentDirectory, port);
            engineSettings.PreferEncryption = false;
            engineSettings.AllowedEncryption = EncryptionTypes.All;

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
        /// <param name="input">The Magnet link or Torrent file location.</param>
        public void QueueTorrent(string input)
        {
            TorrentManagerFactory factory;

            if (input.StartsWith("magnet:"))
            {
                factory = new MagnetTorrentManagerFactory();
                //TODO: magnet links are not working. at least they don't seem to. it's as if it doesn't get the proper announce list. also, it doesn't seem to save the torrent.
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

            var manager = factory.CreateTorrent(input);
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
