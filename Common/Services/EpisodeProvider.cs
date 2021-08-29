using CodeHollow.FeedReader;
using Common.Helpers;
using Common.Models;
using Common.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Services
{
    public class FeedErrorEventArgs : EventArgs
    {
        public FeedErrorEventArgs(string feedUrl, Exception exception)
        {
            FeedUrl = feedUrl;
            Exception = exception;
        }

        public string FeedUrl { get; }
        public Exception Exception { get; }
    }

    public class EpisodeProvider
    {
        private HttpClient HttpClient { get; }

        private MemoryCache<string, Feed> FeedCache { get; }
            = new MemoryCache<string, Feed>(TimeSpan.FromMinutes(30));

        public EpisodeProvider(HttpClient httpClient)
        {
            HttpClient = httpClient;

            FeedUrls.CollectionChanged += FeedUrls_CollectionChanged;
        }

        private async void FeedUrls_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                await RefreshEpisodes();
            }
            catch (Exception)
            {

            }
        }

        private CancellationTokenSource EposidesTokenSource = new CancellationTokenSource();
        private TaskCompletionSource<object> EpisodesTaskSource;
        private readonly object LockObject = new object();
        private async Task RefreshEpisodes()
        {
            Task<object> toWait = null;
            lock (LockObject)
            {
                if (EpisodesTaskSource != null)
                {
                    EposidesTokenSource.Cancel();
                    toWait = EpisodesTaskSource.Task;
                }
            }

            if (toWait != null)
                await toWait;

            lock (LockObject)
            {
                if (EpisodesTaskSource != null)
                    return;
                EpisodesTaskSource = new TaskCompletionSource<object>();
            }

            try
            {
                EposidesTokenSource = new CancellationTokenSource();
                var cancellationToken = EposidesTokenSource.Token;

                await Task.Delay(500);
                if (cancellationToken.IsCancellationRequested)
                    return;

                _Episodes.Clear();
                int progress = 0;
                foreach (var url in FeedUrls)
                {
                    LoadProgress?.Invoke(this, (progress, FeedUrls.Count));
                    try
                    {
                        var feed = await FeedCache.GetAsync(url, () => FeedReader.ReadAsync(url, cancellationToken));
                        var thumbnail = await ImageHelpers.GetThumbnailFromUrl(HttpClient, feed.ImageUrl, 96, 96);
                        foreach (var item in feed.Items)
                        {
                            var episode = new EpisodeModel();
                            episode.Title = item.Title;
                            episode.Description = item.Description;
                            episode.Thumbnail = thumbnail;
                            episode.FeedName = feed.Title;
                            episode.DownloadUrl = item.GetDownloadUrl();
                            if (item.PublishingDate.HasValue)
                                episode.PublishingDate = item.PublishingDate.Value;
                            else
                            {
                                if (DateTimeHelpers.TryUniversalParseDate(item.PublishingDateString, out var dateTime))
                                    episode.PublishingDate = dateTime;
                            }

                            lock (Episodes)
                            {
                                var index = 0;
                                while (Episodes.Count > index &&
                                    _Episodes[index].PublishingDate > episode.PublishingDate)
                                {
                                    index++;
                                }
                                _Episodes.Insert(index, episode);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException || ex is TaskCanceledException)
                            throw;
                        FeedError?.Invoke(this, new FeedErrorEventArgs(url, ex));
                    }
                    progress++;
                }
                LoadProgress?.Invoke(this, (0, 0));
            }
            finally
            {
                var source = EpisodesTaskSource;
                lock (LockObject)
                {
                    EpisodesTaskSource = null;
                }
                source?.TrySetResult(new object());
            }
        }

        public ObservableCollection<string> FeedUrls { get; }
            = new ObservableCollection<string>();

        private ObservableCollection<EpisodeModel> _Episodes
            = new ObservableCollection<EpisodeModel>();
        public ObservableCollection<EpisodeModel> Episodes => _Episodes;

        public event EventHandler<FeedErrorEventArgs> FeedError;

        public event EventHandler<(int, int)> LoadProgress;
    }
}
