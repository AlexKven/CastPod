using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SyncPod.Controls
{
    public class SKBitmapControl : SKElement
    {
        public SKBitmapControl()
        {
            PaintSurface += SKBitmapControl_PaintSurface;
        }

        private void SKBitmapControl_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            e.Surface.Canvas.Clear(SKColors.Transparent);
            if (Bitmap != null)
            {
                e.Surface.Canvas.DrawBitmap(Bitmap, new SKRect(0, 0, e.Info.Width, e.Info.Height));
            }
        }

        private void OnBitmapChanged()
        {
            InvalidateVisual();
        }

        public static readonly DependencyProperty BitmapProperty = DependencyProperty.Register(
            nameof(Bitmap), typeof(SKBitmap), typeof(SKBitmapControl), new PropertyMetadata(
                null, new PropertyChangedCallback((s, e) => (s as SKBitmapControl)?.OnBitmapChanged())));
        public SKBitmap Bitmap
        {
            get => (SKBitmap)GetValue(BitmapProperty);
            set => SetValue(BitmapProperty, value);
        }
    }
}
