using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SyncPod.Converters
{
    public class DownloadStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return new SolidColorBrush(str switch
                {
                    "Downloaded" => Colors.DarkGreen,
                    "Failed" => Colors.Crimson,
                    "Downloading" => Colors.DarkGoldenrod,
                    _ => Colors.Black
                });
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
