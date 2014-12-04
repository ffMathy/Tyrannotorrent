using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoTorrent.Client;
using MonoTorrent.Common;
using System.IO;
using Tyrannotorrent.Helpers;

namespace Tyrannotorrent.Factories
{
    class TorrentFileTorrentManagerFactory : TorrentManagerFactory
    {
#pragma warning disable CS1998
        public override async Task<TorrentManager> CreateTorrent(string file)
#pragma warning restore CS1998
        {

            var torrentContainerPath = StorageHelper.TorrentsPath;
            var torrentPath = Path.Combine(torrentContainerPath, Path.GetFileName(file));
            File.Move(file, torrentPath);

            var torrent = Torrent.Load(torrentPath);
            var sanitizedName = SanitizeFilePath(torrent.Name);
            var savePath = Path.Combine(Environment.CurrentDirectory, sanitizedName);
            return new TorrentManager(torrent, savePath, TorrentSettings);

        }
    }
}
