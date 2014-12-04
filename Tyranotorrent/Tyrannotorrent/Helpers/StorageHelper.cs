using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tyrannotorrent.Helpers
{
    static class StorageHelper
    {
        public static string GetPath(params string[] chunks)
        {
            var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var newChunkArray = new string[chunks.Length + 1];
            newChunkArray[0] = applicationDataPath;

            Array.Copy(chunks, 0, newChunkArray, 1, chunks.Length);

            var path = Path.Combine(newChunkArray);
            Directory.CreateDirectory(path);

            return path;
        }

        public static string TorrentsPath
        {
            get
            {
                return GetPath("Torrents");
            }
        }

        public static string DownloadsPath
        {
            get
            {
                var downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                Directory.CreateDirectory(downloadPath);

                return downloadPath;
            }
        }
    }
}
