using SharpDX;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace ColorManagementSample
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static ImagingFactory factory;
        private static ColorContext sRGBContext;

        static MainPage()
        {
            factory = new ImagingFactory();
            sRGBContext = new ColorContext(factory);
            sRGBContext.InitializeFromExifColorSpace(1);
        }

        public MainPage()
        {
            this.InitializeComponent();

            DisplayInformation.GetForCurrentView().ColorProfileChanged += async (_, __) =>
            {
                await this.SetImageAsync(currentFile);
            };
        }

        private async void OpenNewWindowClick(object sender, RoutedEventArgs e)
        {
            var currentViewId = ApplicationView.GetForCurrentView().Id;
            var newViewId = default(int);
            await CoreApplication.CreateNewView().Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    var frame = new Frame();
                    frame.Navigate(typeof(MainPage));
                    Window.Current.Content = frame;

                    newViewId = ApplicationView.GetForCurrentView().Id;
                });
            await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
        }

        private async void OpenImageClick(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker
                {
                    SettingsIdentifier = "OpenImagePicker",
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                };
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");
            var file = await picker.PickSingleFileAsync();
            await SetImageAsync(file);
        }

        private StorageFile currentFile;

        private async Task SetImageAsync(StorageFile file)
        {
            if (file == null) return;
            this.currentFile = file;

            // モニタプロファイルのStreamを作成
            // ※物理モニタがない環境だと例外を吐く
            var profileStream = await DisplayInformation.GetForCurrentView().GetColorProfileAsync();

            // Stream → Bytes
            var profileBytes = new byte[profileStream.Size];
            var reader = new DataReader(profileStream);
            await reader.LoadAsync((uint)profileStream.Size);
            reader.ReadBytes(profileBytes);

            // モニタプロファイルのColorContextを作成
            var factory = new ImagingFactory(); // 割とあちこちで使う
            var displayProfile = new ColorContext(factory);
            displayProfile.InitializeFromMemory(DataStream.Create(profileBytes, true, false));

            using (var stream = await currentFile.OpenAsync(FileAccessMode.Read))
            {
                // デコーダーでファイルからフレームを取得
                var decoder = new BitmapDecoder(factory, stream.AsStream(), DecodeOptions.CacheOnDemand);
                if (decoder.FrameCount < 1) return;
                var frame = decoder.GetFrame(0);

                // 埋め込みプロファイル取得
                var srcColorContexts = frame.TryGetColorContexts(factory);
                var untaggedOrUnsupported = srcColorContexts == null || srcColorContexts.Length < 1;
                // プロファイルが読み込めなかった場合はsRGBとみなす
                var sourceProfile = !untaggedOrUnsupported ? srcColorContexts[0] : sRGBContext;

                SharpDX.WIC.BitmapSource transformSource = frame;
                if (untaggedOrUnsupported)
                {
                    // プロファイルが読み込めなかった場合はsRGBを適用したいので、FormatConverterで32bppPBGRAへ変換
                    // 変換しなかった場合、色変換時にCMYK画像をsRGBとして扱ってしまうことでエラーが発生する
                    var converter = new FormatConverter(factory);
                    converter.Initialize(frame, PixelFormat.Format32bppPBGRA);
                    transformSource = converter;
                }
                // ColorTransformを通すことで色変換ができる
                var transform = new ColorTransform(factory);
                transform.Initialize(transformSource, sourceProfile, displayProfile, PixelFormat.Format32bppPBGRA);

                var stride = transform.Size.Width * 4;    // 横1行のバイト数
                var size = stride * transform.Size.Height;
                var bytes = new byte[size];
                transform.CopyPixels(bytes, stride); // Byte配列にピクセルデータをコピー

                // ピクセルデータをWriteableBitmapに書き込み
                var bitmap = new WriteableBitmap(transform.Size.Width, transform.Size.Height);
                using (var s = bitmap.PixelBuffer.AsStream())
                {
                    await s.WriteAsync(bytes, 0, size);
                }
                this.Image1.Source = bitmap;
            }
        }
    }
}
