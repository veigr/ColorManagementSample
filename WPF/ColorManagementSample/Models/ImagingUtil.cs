using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ColorManagementSample.Models
{
    internal static class ImagingUtil
    {
        public static BitmapSource CreateColorConvertedBitmap(string filePath)
        {
            var frame = ReadFrame(filePath);
            return new ColorConvertedBitmap(
                frame,
                frame.GetSourceColorContext(),
                Application.Current.MainWindow.GetCurrentMonitorInfo().MonitorProfile,
                PixelFormats.Pbgra32);
        }

        public static BitmapFrame ReadFrame(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath,
                    FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    return BitmapFrame.Create(stream,
                        BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.PreservePixelFormat,
                        BitmapCacheOption.OnLoad);
                }
            }
            catch (IOException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        public static ColorContext GetSourceColorContext(this BitmapFrame frame)
        {
            if (frame.ColorContexts != null && frame.ColorContexts.Any())
                return frame.ColorContexts[0];
            return new ColorContext(PixelFormats.Pbgra32);
        }
    }
}