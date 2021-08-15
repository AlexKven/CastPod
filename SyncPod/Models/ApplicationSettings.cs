using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncPod.Models
{
    public class ApplicationSettings
    {
        public string DownloadLocation { get; set; }

        public List<string> PodcastFeedUrls { get; set; }
    }
}
