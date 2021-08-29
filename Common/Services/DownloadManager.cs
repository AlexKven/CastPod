using Common.Helpers;
using Common.Models;
using Common.Utilities;
using Common.ViewModels;
using MvvmHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Common.Services
{
    public class DownloadManager : ObservableObject
    {
        private class DownloadCommand : ICommand
        {
            private DownloadManager DownloadManager { get; }

            public DownloadCommand(DownloadManager downloadManager)
            {
                DownloadManager = downloadManager;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => true;

            public async void Execute(object parameter)
            {
                if (parameter is EpisodeViewModel episode)
                {
                    DownloadManager.DownloadEpisode(episode);
                }
            }
        }

        private class DownloadOperation
        {
            private EpisodeViewModel Episode { get; }
            private string FileName { get; }
            private WebClient Client { get; } = new WebClient();
            public DownloadOperation(EpisodeViewModel episode, string fileName)
            {
                Episode = episode;
                FileName = fileName;

                Client.DownloadProgressChanged += Client_DownloadProgressChanged;
                Client.DownloadFileCompleted += Client_DownloadFileCompleted;
            }

            public void Cancel()
            {
                if (Client.IsBusy)
                {
                    Client.CancelAsync();
                }
            }

            public void Download()
            {
                if (Client.IsBusy)
                {
                    Client.CancelAsync();
                }
                Episode.IsDownloaded = true;
                Episode.DownloadProgress = 0;
                Episode.DownloadFailed = false;
                Client.DownloadFileAsync(new Uri(Episode.DownloadUrl), $"{FileName}.part");
            }

            private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
            {
                if (e.Error != null)
                {
                    Episode.IsDownloaded = false;
                    Episode.DownloadFailed = true;
                    MessageBox.Show(e.Error.Message, $"Error downloading {Episode.Title}");
                }
                else
                {
                    Episode.IsDownloaded = true;
                    Episode.DownloadFailed = false;
                    var ext = FeedHelpers.GetFileExtensionFromUrl(Episode.DownloadUrl);
                    File.Move($"{FileName}.part", $"{FileName}{ext}");
                }
                Episode.DownloadProgress = 100;
            }

            private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
            {
                Episode.DownloadProgress = e.ProgressPercentage;
            }
        }

        HashSet<char> InvalidPathChars = Path.GetInvalidPathChars().ToHashSet();
        HashSet<char> InvalidFileNameChars = Path.GetInvalidFileNameChars().ToHashSet();

        public DownloadManager()
        {
            InvalidPathChars.Add(':');
            InvalidFileNameChars.Add(':');
        }

        private Dictionary<EpisodeViewModel, (EpisodeModel, DownloadOperation)> Episodes { get; }
            = new Dictionary<EpisodeViewModel, (EpisodeModel, DownloadOperation)>();

        public EpisodeViewModel AddEpisode(EpisodeModel episode)
        {
            var result = new EpisodeViewModel(episode, new DownloadCommand(this));
            Episodes[result] = (episode, null);
            AddEpisodeViewModel(result);
            return result;
        }

        public bool RemoveEpisode(EpisodeViewModel episode)
        {
            return Episodes.Remove(episode);
        }

        private string _FolderPath = "";
        public string FolderPath
        {
            get => _FolderPath;
            set
            {
                var changed = _FolderPath != value;
                if (changed)
                {
                    _FolderPath = value;
                    SetProperty(ref _FolderPath, value);
                    RefreshDownloads();
                }
            }
        }

        private bool _FolderError = true;
        public bool FolderError
        {
            get => _FolderError;
            private set
            {
                SetProperty(ref _FolderError, value);
                foreach (var episode in Episodes.Keys)
                {
                    episode.CanDownload = !FolderError && !string.IsNullOrEmpty(episode.DownloadUrl);
                }
            }
        }

        private Dictionary<string, string> FolderLookup { get; }
            = new Dictionary<string, string>();

        private MemoryCache<string, string[]> DownloadsCache { get; }
            = new MemoryCache<string, string[]>();

        private void AddEpisodeViewModel(EpisodeViewModel episode)
        {
            if (FolderError)
            {
                return;
            }
            var folderName = GetValidFolderName(episode.FeedName);
            var fileName = GetValidFileName(episode.Title);
            var downloads = DownloadsCache.Get(folderName, () =>
            {
                if (FolderLookup.TryGetValue(folderName, out var subFolder))
                {
                    var files = Directory.GetFiles(subFolder);
                    return files
                    .Where(f => Path.GetExtension(f) != ".part")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .ToArray();
                }
                else
                {
                    subFolder = Path.Combine(FolderPath, folderName);
                    Directory.CreateDirectory(subFolder);
                    FolderLookup.Add(folderName, subFolder);
                    return new string[0];
                }
            });
            if (downloads.Contains(fileName))
            {
                episode.IsDownloaded = true;
                episode.DownloadFailed = false;
            }
            else
            {
                episode.IsDownloaded = false;
                if (FolderLookup.TryGetValue(folderName, out var subFolder)
                    && File.Exists(Path.Combine(subFolder, $"{fileName}.part")))
                    episode.DownloadFailed = true;
            }
            episode.CanDownload = !FolderError && !string.IsNullOrEmpty(episode.DownloadUrl);
        }

        public void RefreshDownloads()
        {
            FolderError = false;
            try
            {
                if (!Directory.Exists(FolderPath))
                {
                    FolderError = true;
                }
                else
                {
                    DownloadsCache.Clear();
                    FolderLookup.Clear();
                    var subFolders = Directory.GetDirectories(FolderPath);
                    foreach (var subFolder in subFolders)
                    {
                        var folderName = Path.GetFileName(subFolder);
                        if (!FolderLookup.ContainsKey(folderName))
                        {
                            FolderLookup.Add(folderName, subFolder);
                        }
                    }
                    foreach (var episode in Episodes)
                    {
                        AddEpisodeViewModel(episode.Key);
                    }
                }
            }
            catch (Exception)
            {
                FolderError = true;
            }
        }

        private string GetValidFolderName(string feedName)
        {
            return new string(feedName.Where(c => !InvalidPathChars.Contains(c)).ToArray());
        }

        private string GetValidFileName(string feedName)
        {
            return new string(feedName.Where(c => !InvalidFileNameChars.Contains(c)).ToArray());
        }

        public void DownloadEpisode(EpisodeViewModel episode)
        {
            var entry = Episodes[episode];
            var client = entry.Item2;
            var folder = FolderLookup[GetValidFolderName(episode.FeedName)];
            var fileName = GetValidFileName(episode.Title);
            client?.Cancel();
            if (episode.IsDownloading)
            {
                episode.IsDownloaded = false;
                episode.DownloadProgress = 1;
            }
            else if (episode.IsDownloaded)
            {
                var file = Directory.GetFiles(folder).Where(f => Path.GetFileNameWithoutExtension(f) == fileName).FirstOrDefault();
                if (file != null)
                {
                    File.Delete(file);
                }
                episode.IsDownloaded = false;
            }
            else
            {
                client = new DownloadOperation(episode, Path.Combine(folder, fileName));
                entry.Item2 = client;
                client.Download();
            }
        }
    }
}
