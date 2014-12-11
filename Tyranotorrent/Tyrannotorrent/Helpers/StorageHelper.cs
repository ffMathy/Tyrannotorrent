using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tyrannotorrent.Helpers
{
    static class PathHelper
    {
        public static string GetLocalPath(params string[] chunks)
        {
            var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var newChunkArray = new string[chunks.Length + 2];
            newChunkArray[0] = applicationDataPath;
            newChunkArray[1] = "Tyrannotorrent";

            Array.Copy(chunks, 0, newChunkArray, 2, chunks.Length);

            var path = Path.Combine(newChunkArray);
            Directory.CreateDirectory(path);

            return path;
        }

        public static string CurrentExecutablePath
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                return assembly.Location;
            }
        }

        public static string TorrentsPath
        {
            get
            {
                return GetLocalPath("Torrents");
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
