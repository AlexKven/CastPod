using CodeHollow.FeedReader;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Syndication;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace CastPod.Controls
{
    public sealed partial class EpisodeList : UserControl
    {
        class FeedItemWrapper
        {
            private Feed Feed { get; }
            private FeedItem FeedItem { get; }
            public FeedItemWrapper(Feed feed, FeedItem feedItem)
            {
                Feed = feed;
                FeedItem = feedItem;

                if (FeedItem?.PublishingDate.HasValue ?? false)
                    PublishingDate = FeedItem.PublishingDate.Value;
                else if (FeedItem?.PublishingDateString != null)
                {
                    if (DateTime.TryParse(FeedItem.PublishingDateString, out var result))
                        PublishingDate = result;
                    else if (TryManualParse(FeedItem.PublishingDateString, out result))
                        PublishingDate = result;
                }
            }

            public DateTime? PublishingDate { get; private set; }

            private static bool TryManualParse(string str, out DateTime dateTime)
            {
                dateTime = default;
                var curStr = "";

                int? month = null;
                int? day = null;
                int? year = null;

                var wordSeparators = new HashSet<char>()
                {
                    ' ', ',', '.', ';', ';', '\\', '/', '-'
                };

                bool nextWord(ref DateTime dateTime)
                {
                    var monthStr = curStr.ToLower() switch
                    {
                        "january" or "jan" => 1,
                        "february" or "feb" => 2,
                        "march" or "mar" => 3,
                        "april" or "apr" => 4,
                        "may" => 5,
                        "june" or "jun" => 6,
                        "july" or "jul" => 7,
                        "august" or "aug" => 8,
                        "september" or "sep" => 9,
                        "october" or "oct" => 10,
                        "november" or "nov" => 11,
                        "december" or "dec" => 12,
                        _ => 0
                    };
                    if (monthStr > 0)
                    {
                        month = monthStr;
                    }
                    else if (int.TryParse(curStr, out var num))
                    {
                        if (num > 31)
                            year = num;
                        else if (!day.HasValue)
                        {
                            day = num;
                        }
                        else if (!month.HasValue && num <= 12)
                        {
                            month = num;
                        }

                    }
                    curStr = "";

                    if (day.HasValue && month.HasValue && year.HasValue)
                    {
                        try
                        {
                            dateTime = new DateTime(year.Value, month.Value, day.Value);
                            return true;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                    return false;
                }

                foreach (var chr in str)
                {
                    if (wordSeparators.Contains(chr))
                    {
                        if (nextWord(ref dateTime))
                            return true;
                    }
                    else
                    {
                        curStr += chr;
                    }
                }
                return nextWord(ref dateTime);
            }

            public string Title => FeedItem?.Title;
            public string Description => FeedItem.Description;
            public string ImageUrl => Feed.ImageUrl;
            public string FeedSummary => $"{Feed?.Title} | {PublishingDate?.ToShortDateString()}";
            public bool IsDownloaded { get; set; }
        }

        public EpisodeList()
        {
            this.InitializeComponent();
            HttpClient = new HttpClient();
            _FeedURLs.CollectionChanged += _FeedURLs_CollectionChanged;
            FeedURLs.Add("http://feeds.feedburner.com/netRocksFullMp3Downloads");
            FeedURLs.Add("https://feeds.megaphone.fm/WWO8086402096");
            FeedURLs.Add("http://www.espn.com/espnradio/podcast/feeds/itunes/podCast?id=14554755");
            FeedURLs.Add("https://feeds.megaphone.fm/theweeds");
            FeedURLs.Add("https://feeds.megaphone.fm/VMP5705694065");
        }

        private ObservableCollection<FeedItemWrapper> Episodes { get; }
            = new ObservableCollection<FeedItemWrapper>();

        private ObservableCollection<string> _FeedURLs
            = new ObservableCollection<string>();

        public IList<string> FeedURLs => _FeedURLs;

        private HttpClient HttpClient { get; }
        private CancellationTokenSource EposidesTokenSource = new CancellationTokenSource();
        private TaskCompletionSource<object> EpisodesTaskSource;
        private readonly object LockObject = new object();
        private async void _FeedURLs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                await RefreshEpisodes();
            }
            catch (Exception)
            {

            }
        }

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

                Episodes.Clear();
                foreach (var url in FeedURLs)
                {
                    var feed = await FeedReader.ReadAsync(url, cancellationToken);

                    foreach (var item in feed.Items)
                    {
                        var episode = new FeedItemWrapper(feed, item);
                        lock(Episodes)
                        {
                            var index = 0;
                            while (Episodes.Count > index &&
                                Episodes[index].PublishingDate > episode.PublishingDate)
                            {
                                index++;
                            }
                            Episodes.Insert(index, episode);
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
    }
}
