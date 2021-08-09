using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Common.Helpers
{
    public static class ImageHelpers
    {
        public static async Task<SKBitmap> GetThumbnailFromUrl(HttpClient httpClient, string url, int width, int height)
        {
            var response = await httpClient.GetAsync(url);
            var bitmap = new SKBitmap(width, height);

            if (response.StatusCode == HttpStatusCode.OK) //Make sure the URL is not empty and the image is there
            {
                // download the bytes
                byte[] stream = await response.Content.ReadAsByteArrayAsync();

                // decode the bitmap stream
                var resourceBitmap = SKBitmap.Decode(stream);

                if (resourceBitmap != null)
                {
                    bitmap = resourceBitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.High); //Resize to the canvas
                }
            }
            return bitmap;
        }
    }
}
