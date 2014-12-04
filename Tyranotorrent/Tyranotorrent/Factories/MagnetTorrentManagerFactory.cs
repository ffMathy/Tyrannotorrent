using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoTorrent.Client;
using MonoTorrent;
using System.IO;

namespace Tyranotorrent.Factories
{
    class MagnetTorrentManagerFactory : TorrentManagerFactory
    {
        public override TorrentManager CreateTorrent(string input)
        {
            var magnetLink = new MagnetLink(input);
            var sanitizedName = SanitizeFilePath(magnetLink.Name);
            var savePath = Path.Combine(Environment.CurrentDirectory, sanitizedName);
            var torrentSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", sanitizedName + ".torrent");
            return new TorrentManager(magnetLink, savePath, TorrentSettings, torrentSavePath);
        }
    }
}
