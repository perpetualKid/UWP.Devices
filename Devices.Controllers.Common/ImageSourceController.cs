using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Devices.Controllers.Base;
using Devices.Util.Extensions;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace Devices.Controllers.Common
{
    public class ImageSourceController: ControllerBase
    {
        private ObservableCollection<BitmapImage> images;
        private BitmapImage currentImage;
        private const int maxImages = 10;

        public event EventHandler<EventArgs> OnImageReceived;
        private ObservableCollection<string> supportedFormats;


        public ImageSourceController(string name): base(name)
        {
            images = new ObservableCollection<BitmapImage>();
            supportedFormats = new ObservableCollection<string>();
        }

        public ObservableCollection<BitmapImage> CachedImages
        {
            get { return this.images; }
        }

        public ObservableCollection<string> SupportedFormats
        {
            get { return this.supportedFormats; }
        }

        public BitmapImage CurrentImage { get { return this.currentImage; } }

        public async Task CaptureDeviceImage()
        {
            JsonObject imageCapture = new JsonObject();
            imageCapture.AddValue("Target", "FrontCamera");
            imageCapture.AddValue("Action", "Capture");
            await ControllerHandler.Connection.Send(nameof(ImageSourceController), imageCapture);

        }

        public async Task GetSupportedModesRequest()
        {
            JsonObject imageCapture = new JsonObject();
            imageCapture.AddValue("Target", "FrontCamera");
            imageCapture.AddValue("Action", "ListFormats");
            await ControllerHandler.Connection.Send(nameof(ImageSourceController), imageCapture);
        }

        [TargetAction("FrontCamera", "ListFormats")]
        private async Task GetSupportedModesResponse(JsonObject data)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                supportedFormats.Clear();
                JsonArray formats = data.GetNamedArray("MediaFormat");
                foreach (var item in formats)
                {
                    supportedFormats.Add(item.Stringify());
                }
            });
        }

        [TargetAction("FrontCamera", "Capture")]
        private async Task GetCapturedImage(JsonObject data)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                byte[] buffer = Convert.FromBase64String(data.GetNamedString("ImageBase64"));
                BitmapImage image = new BitmapImage();
                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    await stream.WriteAsync(buffer.AsBuffer());
                    stream.Seek(0);
                    await image.SetSourceAsync(stream);
                }

                currentImage = image;
                while (images.Count >= maxImages)
                    images.RemoveAt(maxImages - 1);
                images.Insert(0, image);
                OnImageReceived?.Invoke(this, EventArgs.Empty);
            });
        }

    }
}
