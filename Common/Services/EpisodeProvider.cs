using CodeHollow.FeedReader;
using Common.Helpers;
using Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Services
{
    public class EpisodeProvider
    {
        private HttpClient HttpClient { get; }

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
                foreach (var url in FeedUrls)
                {
                    var feed = await FeedReader.ReadAsync(url, cancellationToken);
                    var thumbnail = await ImageHelpers.GetThumbnailFromUrl(HttpClient, feed.ImageUrl, 96, 96);
                    foreach (var item in feed.Items)
                    {
                        var episode = new EpisodeModel();
                        episode.Title = item.Title;
                        episode.Description = item.Description;
                        episode.Thumbnail = thumbnail;
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
    }
}
