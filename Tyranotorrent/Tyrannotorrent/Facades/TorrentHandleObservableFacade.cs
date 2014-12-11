using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using Ragnar;
using System.Windows;
using System.IO;
using Tyrannotorrent.Helpers;
using System.Windows.Input;
using Tyrannotorrent.Commands;

namespace Tyrannotorrent.Facades
{
    class TorrentHandleViewModelFacade : INotifyPropertyChanged, IDisposable
    {

        public delegate void TorrentDownloadedEvent(TorrentHandleViewModelFacade torrent);
        public event TorrentDownloadedEvent TorrentDownloaded;

        private LinkedList<int> lastDownloadSpeeds;

        private int averageDownloadSpeedShortTerm;
        private int averageDownloadSpeedLongTerm;

        private TorrentHandle torrentHandle;
        private TorrentStatus torrentStatus;

        private Dispatcher mainThreadDispatcher;

        public string TimeLeft
        {
            get
            {
                var totalSeconds = averageDownloadSpeedLongTerm == 0 ? 0 : (torrentHandle.TorrentFile.TotalSize - torrentStatus.TotalDownload) / averageDownloadSpeedLongTerm;

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

                if (timeLeft != null && timeLeft.Length > 1)
                {
                    timeLeft = timeLeft.Substring(0, timeLeft.Length - 1);
                }

                return timeLeft;
            }
        }

        public Visibility StopButtonVisibility
        {
            get;set;
        }

        public Visibility StartButtonVisibility
        {
            get;set;
        }

        public string TorrentFilePath
        {
            get
            {
                return Path.Combine(PathHelper.TorrentsPath, torrentHandle.TorrentFile.Name + ".torrent");
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

                if (bitDownloadSpeed > 1024)
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
                return torrentStatus == null ? 0 : torrentStatus.Progress * 100.0;
            }
        }

        public string Name
        {
            get
            {
                return torrentHandle.TorrentFile == null ? "Loading ..." : torrentHandle.TorrentFile.Name;
            }
        }

        public string TorrentSavePath
        {
            get
            {
                return Path.Combine(PathHelper.DownloadsPath, torrentHandle.TorrentFile.Name);
            }
        }

        public TorrentHandle TorrentHandle
        {
            get { return torrentHandle; }
        }

        public TorrentHandleViewModelFacade(TorrentHandle torrentManager)
        {
            this.lastDownloadSpeeds = new LinkedList<int>();
            this.mainThreadDispatcher = Dispatcher.CurrentDispatcher;
            this.torrentHandle = torrentManager;

            StopButtonVisibility = Visibility.Visible;
            StartButtonVisibility = Visibility.Collapsed;

            StartUpdateLoop();
        }

        private async void StartUpdateLoop()
        {
            while (true)
            {
                await Task.Delay(100);

                if (torrentHandle.IsPaused)
                {
                    lastDownloadSpeeds.AddFirst(0);
                }
                else
                {

                    var oldTorrentStatus = torrentStatus;
                    torrentStatus = torrentHandle.QueryStatus();

                    var currentSpeed = torrentStatus.DownloadRate;
                    lastDownloadSpeeds.AddFirst(currentSpeed);

                }

                while (lastDownloadSpeeds.Count > 1000 * 6)
                {
                    lastDownloadSpeeds.RemoveLast();
                }

                averageDownloadSpeedShortTerm = (int)(lastDownloadSpeeds.Count > 0 ? lastDownloadSpeeds.Take(10).Average() : 0);
                averageDownloadSpeedLongTerm = (int)(lastDownloadSpeeds.Count > 0 ? lastDownloadSpeeds.Average() : 0);

                NotifyPropertyChanged("DownloadSpeed");
                NotifyPropertyChanged("Progress");
                NotifyPropertyChanged("TimeLeft");
                NotifyPropertyChanged("Name");

                //are we done?
                if (Progress == 100)
                {

                    mainThreadDispatcher.Invoke(delegate
                    {
                        if (TorrentDownloaded != null)
                        {
                            TorrentDownloaded(this);
                        }
                    });

                    break;
                }
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

        public event PropertyChangedEventHandler PropertyChanged;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                //save resume data if needed.
                if (torrentHandle.NeedSaveResumeData())
                {
                    torrentHandle.SaveResumeData();
                }

                if (disposing)
                {
                    torrentHandle.Dispose();
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
