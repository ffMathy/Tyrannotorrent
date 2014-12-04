using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoTorrent.Client;
using MonoTorrent.Common;
using System.IO;

namespace Tyrannotorrent.Factories
{
    class TorrentFileTorrentManagerFactory : TorrentManagerFactory
    {
        public override async Task<TorrentManager> CreateTorrent(string input)
        {
            var torrent = Torrent.Load(input);
            var sanitizedName = SanitizeFilePath(torrent.Name);
            var savePath = Path.Combine(Environment.CurrentDirectory, sanitizedName);
            return new TorrentManager(torrent, savePath, TorrentSettings);
        }
    }
}
