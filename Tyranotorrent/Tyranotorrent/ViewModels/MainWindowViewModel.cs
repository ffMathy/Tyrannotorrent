using MonoTorrent.Client;
using MonoTorrent.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tyranotorrent.ViewModels
{
    class MainWindowViewModel
    {

        public IEnumerable<TorrentManager> Downloads { get; set; }

    }
}
