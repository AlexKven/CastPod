using Common.Models;
using MvvmHelpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Common.ViewModels
{
    public class EpisodeViewModel : ObservableObject
    {
        public ICommand DownloadCommand { get; }

        public EpisodeViewModel(
            EpisodeModel episode,
            ICommand downloadCommand)
        {
            DownloadCommand = downloadCommand;

            PublishingDate = episode.PublishingDate;
            Title = episode.Title;
            Description = episode.Description;
            Thumbnail = episode.Thumbnail;
            FeedName = episode.FeedName;
            DownloadUrl = episode.DownloadUrl;
        }

        public DateTime? PublishingDate { get; }
        public string Title { get; }
        public string Description { get; }
        public SKBitmap Thumbnail { get; }
        public string FeedName { get; }
        public string DownloadUrl { get; }
        public string FeedSummary => $"{FeedName} | {PublishingDate?.ToShortDateString()}";

        private bool _IsDownloaded = false;
        public bool IsDownloaded
        {
            get => _IsDownloaded;
            set
            {
                SetProperty(ref _IsDownloaded, value);
                OnPropertyChanged(nameof(DownloadText));
                OnPropertyChanged(nameof(DownloadStatus));
            }
        }

        private bool _CanDownload = false;
        public bool CanDownload
        {
            get => _CanDownload;
            set => SetProperty(ref _CanDownload, value);
        }

        private int _DownloadProgress = 1;
        public int DownloadProgress
        {
            get => _DownloadProgress;
            set
            {
                SetProperty(ref _DownloadProgress, value);
                IsDownloading = DownloadProgress < 100;
                OnPropertyChanged(nameof(DownloadText));
                OnPropertyChanged(nameof(DownloadStatus));
            }
        }

        private bool _IsDownloading = false;
        public bool IsDownloading
        {
            get => _IsDownloading;
            private set => SetProperty(ref _IsDownloading, value);
        }

        public string DownloadText => DownloadFailed ? "Retry" : IsDownloaded ? IsDownloading ? $"Cancel ({DownloadProgress}%)" : "Delete" : "Download";
        public string DownloadStatus => DownloadFailed ? "Failed" : IsDownloaded ? IsDownloading ? $"Downloading" : "Downloaded" : "Not Downloaded";

        private bool _DownloadFailed = false;
        public bool DownloadFailed
        {
            get => _DownloadFailed;
            set
            {
                SetProperty(ref _DownloadFailed, value);
                OnPropertyChanged(nameof(DownloadText));
                OnPropertyChanged(nameof(DownloadStatus));
            }
        }
    }
}
