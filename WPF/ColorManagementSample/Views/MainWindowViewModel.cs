using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using ColorManagementSample.Models;
using Livet;

namespace ColorManagementSample.Views
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

        #region ImageFilePath�ύX�ʒm�v���p�e�B
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

        #region ImageSource�ύX�ʒm�v���p�e�B
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