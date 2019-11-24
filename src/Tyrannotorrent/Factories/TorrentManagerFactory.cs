namespace Tyrannotorrent.Factories
{
    using Ragnar;
    using System.Threading.Tasks;

    abstract class TorrentManagerFactory
    {

        public abstract Task<TorrentHandle> CreateTorrent(Session session, string input);

        protected TorrentManagerFactory()
        {
        }

        protected TorrentHandle AddTorrent(Session session, AddTorrentParams addTorrentParams)
        {
            //set the maximum upload to 16 kilobits per second.
            addTorrentParams.UploadLimit = 16 * 1024;

            return session.AddTorrent(addTorrentParams);
        }
    }
}
