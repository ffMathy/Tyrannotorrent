using Ragnar;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tyrannotorrent.Helpers;

namespace Tyrannotorrent.Factories
{
    class MagnetTorrentManagerFactory : TorrentManagerFactory
    {

#pragma warning disable CS1998
        public override async Task<TorrentHandle> CreateTorrent(Session session, string input)
#pragma warning restore CS1998
        {
            var magnetTorrentHandle = session.AddTorrent(new AddTorrentParams()
            {
                Url = input
            });

            //wait for metadata.
            while (!magnetTorrentHandle.HasMetadata)
            {
                await Task.Delay(100);
            }

            var torrentFile = magnetTorrentHandle.TorrentFile;

            //now create a proper torrent file.
            var torrentPath = Path.Combine(PathHelper.TorrentsPath, torrentFile.Name + ".torrent");
            if (!File.Exists(torrentPath))
            {
                using (var creator = new TorrentCreator(torrentFile))
                {
                    var torrentData = creator.Generate();
                    File.WriteAllBytes(torrentPath, torrentData);
                }
            }

            session.RemoveTorrent(magnetTorrentHandle);
            magnetTorrentHandle.Dispose();

            var fileFactory = new TorrentFileTorrentManagerFactory();
            return await fileFactory.CreateTorrent(session, torrentPath);
        }
    }
}
