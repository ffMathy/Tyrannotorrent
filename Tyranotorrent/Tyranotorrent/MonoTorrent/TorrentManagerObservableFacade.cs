using MonoTorrent.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tyranotorrent.MonoTorrent
{
    class TorrentManagerViewModelFacade : INotifyPropertyChanged
    {
        private TorrentManager manager;

        public double Progress
        {
            get
            {
                return manager.Progress;
            }
        }

        public TorrentManagerViewModelFacade(TorrentManager manager)
        {
            this.manager = manager;
            manager.PieceHashed += Manager_PieceHashed;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Manager_PieceHashed(object sender, PieceHashedEventArgs e)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Progress"));
            }
        }
    }
}
