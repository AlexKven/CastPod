using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models
{
    public class EpisodeModel
    {
        public DateTime? PublishingDate { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public SKBitmap Thumbnail { get; set; }

        public string FeedName { get; set; }

        public string DownloadUrl { get; set; }
    }
}
