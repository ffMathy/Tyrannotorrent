using MonoTorrent.Client;
using MonoTorrent.Common;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using MonoTorrent.Client.Tracker;

namespace Tyranotorrent.MonoTorrent
{
    class TorrentManagerViewModelFacade : INotifyPropertyChanged, IDisposable
    {

        public delegate void TorrentDownloadedEvent(TorrentManagerViewModelFacade torrent);
        public event TorrentDownloadedEvent TorrentDownloaded;

        private LinkedList<int> lastDownloadSpeeds;

        private TorrentManager torrentManager;

        private Dispatcher mainThreadDispatcher;

        public TorrentManager TorrentManager
        {
            get { return torrentManager; }
        }

        public string DownloadSpeed
        {
            get
            {
                var downloadSpeed = lastDownloadSpeeds.Count > 0 ? lastDownloadSpeeds.Average() : 0;

                var unit = "B/s";

                if (downloadSpeed > 1024)
                {
                    unit = "KB/s";
                    downloadSpeed /= 1024.0;
                }

                if (downloadSpeed > 1024)
                {
                    unit = "MB/s";
                    downloadSpeed /= 1024.0;
                }

                if (downloadSpeed > 1024)
                {
                    unit = "GB/s";
                    downloadSpeed /= 1024.0;
                }

                if (downloadSpeed > 1024)
                {
                    unit = "TB/s";
                    downloadSpeed /= 1024.0;
                }

                return Math.Round(downloadSpeed, 2) + " " + unit;
            }
        }

        public double Progress
        {
            get
            {
                return torrentManager.Progress;
            }
        }

        public string Name
        {
            get
            {
                var torrent = torrentManager.Torrent;
                if (torrent == null)
                {
                    return Path.GetFileName(torrentManager.SavePath);
                }
                else
                {
                    return torrent.Name;
                }
            }
        }

        public TorrentManagerViewModelFacade(TorrentManager torrentManager)
        {
            this.lastDownloadSpeeds = new LinkedList<int>();
            this.mainThreadDispatcher = Dispatcher.CurrentDispatcher;
            this.torrentManager = torrentManager;

            torrentManager.TorrentStateChanged += Manager_TorrentStateChanged;

            torrentManager.PieceHashed += delegate (object o, PieceHashedEventArgs e)
            {
                var pieceIndex = e.PieceIndex;
                var totalPieces = e.TorrentManager.Torrent.Pieces.Count;
                var hashProgress = (double)pieceIndex / totalPieces * 100.0;
                if (e.HashPassed)
                {
                    Debug.WriteLine("Piece {0} of {1} is complete", pieceIndex, totalPieces);
                }
                else
                {
                    Debug.WriteLine("Piece {0} of {1} is corrupt or incomplete ", pieceIndex, totalPieces);
                }

                Debug.WriteLine("Total hashing progress is: {0}%", hashProgress);

                Debug.WriteLine("{0}% of the torrent download is complete", Progress);
                NotifyPropertyChanged("Progress");

                if (Progress == 100)
                {
                    //we're done! no more seeding please.
                    torrentManager.Stop();

                    mainThreadDispatcher.Invoke(delegate
                    {
                        if (TorrentDownloaded != null)
                        {
                            TorrentDownloaded(this);
                        }
                    });
                }
            };

            foreach (var tier in torrentManager.TrackerManager)
            {
                foreach (var tracker in tier.GetTrackers())
                {
                    tracker.AnnounceComplete += delegate (object sender, AnnounceResponseEventArgs e)
                    {
                        Debug.WriteLine(string.Format("Announce {0}: {1}", e.Successful, e.Tracker.ToString()));
                    };
                }
            }

            StartUpdateLoop();
        }

        private async void StartUpdateLoop()
        {
            while (true)
            {
                await Task.Delay(250);

                var currentSpeed = torrentManager.Monitor.DownloadSpeed;
                lastDownloadSpeeds.AddFirst(currentSpeed);

                while (lastDownloadSpeeds.Count > 100)
                {
                    lastDownloadSpeeds.RemoveLast();
                }

                Update();
            }
        }

        private void Manager_TorrentStateChanged(object sender, TorrentStateChangedEventArgs e)
        {
            Debug.WriteLine(Name + " changed state to from " + e.OldState + " to " + e.NewState);
            if (e.NewState == TorrentState.Metadata)
            {
                NotifyPropertyChanged("Name");
            }
            else if (e.NewState == TorrentState.Stopped)
            {
                var engine = torrentManager.Engine;
                engine.Unregister(torrentManager);
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            mainThreadDispatcher.Invoke(delegate
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            });
        }

        private void Update()
        {
            NotifyPropertyChanged("DownloadSpeed");

            if (Debugger.IsAttached)
            {
                var tracker = torrentManager.TrackerManager.CurrentTracker;
                if (tracker != null)
                {
                    if (!string.IsNullOrEmpty(tracker.WarningMessage))
                    {
                        Debug.WriteLine("Warning: " + tracker.WarningMessage);
                    }

                    if (!string.IsNullOrEmpty(tracker.FailureMessage))
                    {
                        Debug.WriteLine("Error: " + tracker.FailureMessage);
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    torrentManager.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
