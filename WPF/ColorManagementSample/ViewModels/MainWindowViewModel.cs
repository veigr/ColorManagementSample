using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ColorManagementSample.Models;
using Livet;

namespace ColorManagementSample.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        public void Initialize()
        {
            this.ImageFilePath = null;
        }

        public void Drop(object sender, DragEventArgs e)
        {
            if(!(e.Data is DataObject)) return;

            var data = (DataObject) e.Data;
            if(!data.ContainsFileDropList()) return;

            var files = data.GetFileDropList().Cast<string>().ToArray();
            if(!files.Any()) return;

            this.ImageFilePath = files.First();
        }

        public void RefreshImage()
        {
            this.ImageFilePath = this.ImageFilePath;
        }

        #region ImageFilePath変更通知プロパティ
        private string _ImageFilePath;

        public string ImageFilePath
        {
            get
            { return this._ImageFilePath; }
            set
            {
                if (value != null)
                {
                    var image = ImagingUtil.CreateColorConvertedBitmap(value);
                    if (image == null) return;
                    this.ImageSource = image;
                }
                this._ImageFilePath = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        #region ImageSource変更通知プロパティ
        private BitmapSource _ImageSource;

        public BitmapSource ImageSource
        {
            get
            { return this._ImageSource; }
            set
            { 
                if (this._ImageSource == value)
                    return;
                this._ImageSource = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

    }

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
