﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SyncPod.Extensions
{
    // from https://stackoverflow.com/a/9875475/6706737
    public static class PixelBasedScrollingBehavior
    {
        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(PixelBasedScrollingBehavior), new UIPropertyMetadata(false, HandleIsEnabledChanged));

        private static void HandleIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var vsp = d as VirtualizingStackPanel;
            if (vsp == null)
            {
                return;
            }

            var property = typeof(VirtualizingStackPanel).GetProperty("IsPixelBased",
                                                                         BindingFlags.NonPublic | BindingFlags.Instance);

            if (property == null)
            {
                throw new InvalidOperationException("Pixel-based scrolling behaviour hack no longer works!");
            }

            if ((bool)e.NewValue == true)
            {
                property.SetValue(vsp, true, new object[0]);
            }
            else
            {
                property.SetValue(vsp, false, new object[0]);
            }
        }
    }
}
