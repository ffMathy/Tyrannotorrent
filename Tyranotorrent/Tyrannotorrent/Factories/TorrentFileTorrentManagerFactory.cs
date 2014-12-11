using System.IO;
using Tyrannotorrent.Helpers;
using Ragnar;
using System;
using System.Threading.Tasks;

namespace Tyrannotorrent.Factories
{
    class TorrentFileTorrentManagerFactory : TorrentManagerFactory
    {

#pragma warning disable CS1998
        public override async Task<TorrentHandle> CreateTorrent(Session session, string file)
#pragma warning restore CS1998
        {
            var torrentInformation = new TorrentInfo(file);
            var torrentDestinationPath = Path.Combine(PathHelper.TorrentsPath, torrentInformation.Name + ".torrent");
            if (!File.Exists(torrentDestinationPath))
            {
                File.Move(file, torrentDestinationPath);
            }

            var savePath = Path.Combine(PathHelper.DownloadsPath, torrentInformation.Name);
            return AddTorrent(session, new AddTorrentParams()
            {
                SavePath = savePath,
                TorrentInfo = torrentInformation
            });

        }
    }
}
