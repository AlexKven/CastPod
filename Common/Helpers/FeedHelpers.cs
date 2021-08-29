using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Helpers
{
    public static class FeedHelpers
    {
        public static string GetDownloadUrl(this FeedItem item)
        {
            if (item.SpecificItem is MediaRssFeedItem specific0)
            {
                return specific0.Enclosure?.Url;
            }
            if (item.SpecificItem is Rss20FeedItem specific1)
            {
                return specific1.Enclosure?.Url;
            }
            else
            {
                return null;
            }
        }

        // From here:
        // https://stackoverflow.com/a/52748299/6706737
        public static string GetFileExtensionFromUrl(string url)
        {
            url = url.Split('?')[0];
            url = url.Split('/').Last();
            return url.Contains('.') ? url.Substring(url.LastIndexOf('.')) : "";
        }
    }
}
