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
using Tyranotorrent.MonoTorrent;
using System.Collections.Generic;

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

            var random = new Random((int)DateTime.UtcNow.Ticks);
            var port = 25555 + random.Next(-100, 100);

            var engineSettings = new EngineSettings(Environment.CurrentDirectory, port);
            engineSettings.PreferEncryption = false;
            engineSettings.AllowedEncryption = EncryptionTypes.All;

            torrentSettings = new TorrentSettings(1, 50, 0, 1);
            torrentSettings.InitialSeedingEnabled = false;

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
                var sanitizedName = SanitizeFilePath(torrent.Name);
                var savePath = Path.Combine(Environment.CurrentDirectory, sanitizedName);
                manager = new TorrentManager(torrent, savePath, torrentSettings);
            } else if(torrentPath.StartsWith("magnet:"))
            {
                //TODO: magnet links are not working. at least they don't seem to. it's as if it doesn't get the proper announce list. also, it doesn't seem to save the torrent.

                var magnetLink = new MagnetLink(torrentPath);
                var sanitizedName = SanitizeFilePath(magnetLink.Name);
                var savePath = Path.Combine(Environment.CurrentDirectory, sanitizedName);
                var torrentSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", sanitizedName + ".torrent");
                manager = new TorrentManager(magnetLink, savePath, torrentSettings, torrentSavePath);
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

        private string SanitizeFilePath(string input)
        {
            foreach (var illegalCharacter in Path.GetInvalidPathChars().Union(Path.GetInvalidFileNameChars()))
            {
                input = input.Replace(illegalCharacter, '_');
            }
            while (input.Contains("__")) input = input.Replace("__", "_");
            return input;
        }

        private void ViewModel_TorrentDownloaded(TorrentManagerViewModelFacade torrent)
        {
            Downloads.Remove(torrent);
            torrent.Dispose();
        }
    }
}
