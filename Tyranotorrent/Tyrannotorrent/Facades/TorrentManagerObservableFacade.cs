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

namespace Tyrannotorrent.Facades
{
    class TorrentManagerViewModelFacade : INotifyPropertyChanged, IDisposable
    {

        public delegate void TorrentDownloadedEvent(TorrentManagerViewModelFacade torrent);
        public event TorrentDownloadedEvent TorrentDownloaded;

        private LinkedList<int> lastDownloadSpeeds;

        private int averageDownloadSpeedShortTerm;
        private int averageDownloadSpeedLongTerm;

        private TorrentManager torrentManager;

        private Dispatcher mainThreadDispatcher;

        public TorrentManager TorrentManager
        {
            get { return torrentManager; }
        }

        public string State
        {
            get
            {
                var totalSeconds = averageDownloadSpeedLongTerm == 0 ? 0 : (torrentManager.Torrent.Size - torrentManager.Monitor.DataBytesDownloaded) / averageDownloadSpeedLongTerm;

                var minutes = Math.Ceiling(totalSeconds / 60.0 % 60.0);
                var hours = Math.Floor(totalSeconds / 60.0 / 60.0 % 60.0);
                var days = Math.Floor(totalSeconds / 60.0 / 60.0 / 60.0 % 24.0);
                var months = Math.Floor(totalSeconds / 60.0 / 60.0 / 60.0 / 24.0 % 30.5);
                var years = Math.Floor(totalSeconds / 60.0 / 60.0 / 60.0 / 24.0 / 30.5 % 12.0);

                var timeLeft = "";
                if (years > 0)
                {
                    timeLeft += years + "y ";
                }
                if (months > 0)
                {
                    timeLeft += months + "m ";
                }
                if (days > 0 && years == 0)
                {
                    timeLeft += days + "d ";
                }
                if (months == 0)
                {
                    timeLeft += string.Format("{0:00}", hours) + "h ";
                }
                if (days == 0)
                {
                    timeLeft += string.Format("{0:00}", Math.Max(1, minutes)) + "m ";
                }

                if (timeLeft.Length > 0) timeLeft = timeLeft.Substring(0, timeLeft.Length - 1);

                return torrentManager.State + "\n" + timeLeft;
            }
        }

        public string DownloadSpeed
        {
            get
            {
                var byteDownloadSpeed = (double)averageDownloadSpeedShortTerm;
                var bitDownloadSpeed = byteDownloadSpeed * 8;

                var byteUnit = "B/s";
                var bitUnit = "b/s";

                if(bitDownloadSpeed > 1024)
                {
                    bitUnit = "Kb/s";
                    bitDownloadSpeed /= 1024.0;
                }
                if (byteDownloadSpeed > 1024)
                {
                    byteUnit = "KB/s";
                    byteDownloadSpeed /= 1024.0;
                }

                if (bitDownloadSpeed > 1024)
                {
                    bitUnit = "Mb/s";
                    bitDownloadSpeed /= 1024.0;
                }
                if (byteDownloadSpeed > 1024)
                {
                    byteUnit = "MB/s";
                    byteDownloadSpeed /= 1024.0;
                }

                if (bitDownloadSpeed > 1024)
                {
                    bitUnit = "Gb/s";
                    bitDownloadSpeed /= 1024.0;
                }
                if (byteDownloadSpeed > 1024)
                {
                    byteUnit = "GB/s";
                    byteDownloadSpeed /= 1024.0;
                }

                if (bitDownloadSpeed > 1024)
                {
                    bitUnit = "Tb/s";
                    bitDownloadSpeed /= 1024.0;
                }
                if (byteDownloadSpeed > 1024)
                {
                    byteUnit = "TB/s";
                    byteDownloadSpeed /= 1024.0;
                }

                if (bitDownloadSpeed > 1024)
                {
                    bitUnit = "Pb/s";
                    bitDownloadSpeed /= 1024.0;
                }
                if (byteDownloadSpeed > 1024)
                {
                    byteUnit = "PB/s";
                    byteDownloadSpeed /= 1024.0;
                }

                return (byteDownloadSpeed == 0 ? "0" : string.Format("{0:0.00}", byteDownloadSpeed)) + " " + byteUnit + "\n" + (bitDownloadSpeed == 0 ? "0" : string.Format("{0:0.00}", bitDownloadSpeed)) + " " + bitUnit;
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
            var count = 0;
            while (true)
            {
                await Task.Delay(100);

                var currentSpeed = torrentManager.Monitor.DownloadSpeed;
                lastDownloadSpeeds.AddFirst(currentSpeed);

                while (lastDownloadSpeeds.Count > 1000 * 6)
                {
                    lastDownloadSpeeds.RemoveLast();
                }

                count++;
                if (count >= 100)
                {
                    count = 0;
                    torrentManager.SaveFastResume();
                }

                Update();
            }
        }

        private void Manager_TorrentStateChanged(object sender, TorrentStateChangedEventArgs e)
        {
            Debug.WriteLine(Name + " changed state to from " + e.OldState + " to " + e.NewState);
            NotifyPropertyChanged("State");
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
            averageDownloadSpeedShortTerm = (int)(lastDownloadSpeeds.Count > 0 ? lastDownloadSpeeds.Take(10).Average() : 0);
            averageDownloadSpeedLongTerm = (int)(lastDownloadSpeeds.Count > 0 ? lastDownloadSpeeds.Average() : 0);

            NotifyPropertyChanged("DownloadSpeed");
            NotifyPropertyChanged("State");
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
