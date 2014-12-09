using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoTorrent.Client;
using MonoTorrent;
using System.IO;
using MonoTorrent.Common;
using Tyrannotorrent.Helpers;

namespace Tyrannotorrent.Factories
{
    class MagnetTorrentManagerFactory : TorrentManagerFactory
    {
        private readonly ClientEngine engine;

        public MagnetTorrentManagerFactory(ClientEngine engine)
        {
            this.engine = engine;
        }

        public override async Task<TorrentManager> CreateTorrent(string input)
        {
            var magnetLink = new MagnetLink(input);

            var sanitizedName = SanitizeFilePath(magnetLink.Name ?? "Magnet link");
            var savePath = Path.Combine(StorageHelper.DownloadsPath, sanitizedName);
            var torrentSavePath = Path.Combine(StorageHelper.TorrentsPath, sanitizedName + ".torrent");

            var torrentManager = new TorrentManager(magnetLink, savePath, TorrentSettings, torrentSavePath);
            engine.Register(torrentManager);

            torrentManager.Start();

            var dht = engine.DhtEngine;
            dht.GetPeers(magnetLink.InfoHash);
            
            while (torrentManager.State == TorrentState.Stopped) await Task.Delay(1000);
            while (torrentManager.State == TorrentState.Metadata) await Task.Delay(1000);

            torrentManager.Stop();
            torrentManager.Dispose();

            var torrentFactory = new TorrentFileTorrentManagerFactory();
            return await torrentFactory.CreateTorrent(torrentSavePath);
        }
    }
}
