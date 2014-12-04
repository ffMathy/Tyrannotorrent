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

            var sanitizedName = SanitizeFilePath(magnetLink.Name);
            var savePath = Path.Combine(Environment.CurrentDirectory, sanitizedName);
            var torrentSavePath = Path.Combine(StorageHelper.TorrentsPath, sanitizedName + ".torrent");

            var torrentManager = new TorrentManager(magnetLink, savePath, TorrentSettings, torrentSavePath);
            engine.Register(torrentManager);

            torrentManager.Start();

            engine.DhtEngine.GetPeers(magnetLink.InfoHash);

            while (torrentManager.State == TorrentState.Stopped) await Task.Delay(100);
            while (torrentManager.State == TorrentState.Metadata) await Task.Delay(100);

            var torrentFactory = new TorrentFileTorrentManagerFactory();
            return await torrentFactory.CreateTorrent(torrentSavePath);
        }
    }
}
