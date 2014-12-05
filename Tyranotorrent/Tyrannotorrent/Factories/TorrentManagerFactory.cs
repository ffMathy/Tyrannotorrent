namespace Tyrannotorrent.Factories
{
    using MonoTorrent.Client;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using TorrentManager = MonoTorrent.Client.TorrentManager;

    abstract class TorrentManagerFactory
    {

        protected TorrentSettings TorrentSettings { get; private set; }

        public abstract Task<TorrentManager> CreateTorrent(string input);

        protected TorrentManagerFactory()
        {
            var torrentSettings = new TorrentSettings(5, 50, 0, 50);

            this.TorrentSettings = torrentSettings;
        }

        protected string SanitizeFilePath(string input)
        {
            var invalidPathCharacters = Path.GetInvalidPathChars();
            var invalidFileNameCharacters = Path.GetInvalidFileNameChars();

            foreach (var illegalCharacter in invalidPathCharacters.Union(invalidFileNameCharacters))
            {
                input = input.Replace(illegalCharacter, '_');
            }
            while (input.Contains("__")) input = input.Replace("__", "_");
            return input;
        }
    }
}
