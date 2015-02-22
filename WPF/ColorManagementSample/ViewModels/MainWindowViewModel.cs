using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using ColorManagementSample.Models;
using Livet;
using System;

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
                    ////// カラマネする場合
                    var image = ImagingUtil.CreateColorConvertedBitmap(value);

                    //// カラマネしない場合
                    //var image = new BitmapImage(new Uri(value));
                    
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
}
