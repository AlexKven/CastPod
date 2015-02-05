using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CastPod
{
    public sealed partial class PlayerControl : UserControl
    {
        public PlayerControl()
        {
            this.InitializeComponent();
        }

        public async Task Play(string fileName)
        {
            var packageLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var assetsFolder = await packageLocation.GetFolderAsync("assets");
            var soundsFolder = await assetsFolder.GetFolderAsync("soundFiles");
            var myAudio = await soundsFolder.GetFileAsync(fileName); // would be ding.mp3   

            MainMediaElement = new MediaElement();

            var stream = await myAudio.OpenAsync(FileAccessMode.Read);
            MainMediaElement.SetSource(stream, myAudio.ContentType);

            MainMediaElement.Play();
        }

        private async void GoButton_Click(object sender, RoutedEventArgs e)
        {
            await Play(PathBox.Text);
        }  
    }
}
