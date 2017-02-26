using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Devices.Controllers.Base;
using Devices.Util.Collections;
using Devices.Util.Extensions;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
using System.Linq;

namespace Devices.Controllers.Common
{
    public class ImageSourceController: ControllerBase
    {
        public struct ImageFormat
        {
            public string Format;
            public int Width;
            public int Height;
            public JsonObject JsonFormat;

            public override string ToString()
            {
                return $"{Width}*{Height}, {Format}";
            }
        }

        private ObservableCollection<BitmapImage> images;
        private BitmapImage currentImage;
        private string currentFormat;

        private List<ImageFormat> supportedImageFormats;

        public event EventHandler<BitmapImage> OnImageReceived;

        public event EventHandler<ImageFormat> OnCurrentFormatChanged;

        public event EventHandler OnSupportedFormatsChanged;

        public ImageSourceController(string name): base(name)
        {
            images = new ObservableCollection<BitmapImage>();
            supportedImageFormats = new List<ImageFormat>();
        }

        public int ImageCacheSize { get; set; } = 10;

        public ObservableCollection<BitmapImage> CachedImages { get { return this.images; } }

        public List<ImageFormat> SupportedImageFormats { get { return this.supportedImageFormats; } }

        public BitmapImage CurrentImage { get { return this.currentImage; } }

        public IEnumerable<string> GetSupportedCaptureFormats()
        {
            return supportedImageFormats?.Select((formatJson) => formatJson.Format).Distinct() ?? new string[0];
        }

        public IEnumerable<string> GetSupportedCaptureResolutions()
        {
            return supportedImageFormats?.
                OrderByDescending(format => format.Width).
                ThenBy(format => format.Height).
                Select((imageFormat) =>
            {
                return $"{imageFormat.Width}*{imageFormat.Height}";
            }).Distinct() ?? new string[0];
        }

        public IEnumerable<string> GetSupportedCaptureResolutions(string captureFormat)
        {
            return supportedImageFormats?.
                Where(format => (string.IsNullOrEmpty(captureFormat) ? true : format.Format == captureFormat)).
                OrderByDescending(format => format.Width).ThenBy(format => format.Height).
                Select((imageFormat) =>
                {
                    return $"{imageFormat.Width}*{imageFormat.Height}";
                }).Distinct() ?? new string[0];
        }

        public IEnumerable<string> GetAllSupportedFormats()
        {
            return supportedImageFormats?.
                Select((imageFormat) =>
                {
                    return $"Resolution:{imageFormat.Width}*{imageFormat.Height}, CaptureFormat:{imageFormat.Format}, BitRate:{imageFormat.JsonFormat.GetNamedNumber("Bitrate")}, FrameRate:{imageFormat.JsonFormat.GetNamedString("FrameRate")}, PixelAspectRatio:{imageFormat.JsonFormat.GetNamedString("PixelAspectRatio")}";
                })?? new string[0];
        }

        public async Task CaptureDeviceImage()
        {
            JsonObject imageCapture = new JsonObject();
            imageCapture.AddValue("Target", "FrontCamera");
            imageCapture.AddValue("Action", "Capture");
            await ControllerHandler.Connection.Send(nameof(ImageSourceController), imageCapture).ConfigureAwait(false);

        }

        public async Task GetSupportedFormats(string type = null, string subType = null)
        {
            JsonObject imageCapture = new JsonObject();
            imageCapture.AddValue("Target", "FrontCamera");
            imageCapture.AddValue("Action", "ListFormats");
            if (!string.IsNullOrEmpty(type))
                imageCapture.AddValue("Type", type);
            if (!string.IsNullOrEmpty(subType))
                imageCapture.AddValue("SubType", subType);

            await ControllerHandler.Connection.Send(nameof(ImageSourceController), imageCapture).ConfigureAwait(false);
        }

        public async Task GetCurrentFormat()
        {
            JsonObject imageCapture = new JsonObject();
            imageCapture.AddValue("Target", "FrontCamera");
            imageCapture.AddValue("Action", "GetCurrentFormat");
            await ControllerHandler.Connection.Send(nameof(ImageSourceController), imageCapture).ConfigureAwait(false);
        }

        [TargetAction("FrontCamera", "GetCurrentFormat")]
        private Task GetCurrentFormatResponse(JsonObject data)
        {
            OnCurrentFormatChanged?.Invoke(this, JsonFormatToImageFormat(data.GetNamedValue("MediaFormat")));
            return Task.CompletedTask;
        }

        [TargetAction("FrontCamera", "ListFormats")]
        private Task GetAllFormatResponse(JsonObject data)
        {
            supportedImageFormats.Clear();
            JsonArray formats = data.GetNamedArray("MediaFormat");
            foreach (var item in formats)
            {
                supportedImageFormats.Add(JsonFormatToImageFormat(item));
            }
            OnSupportedFormatsChanged?.Invoke(this, new EventArgs());
            return Task.CompletedTask;
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
                while (images.Count >= ImageCacheSize)
                    images.RemoveAt(ImageCacheSize - 1);
                images.Insert(0, image);
                OnImageReceived?.Invoke(this, currentImage);
            });

        }    

        private ImageFormat JsonFormatToImageFormat(IJsonValue format)
        {
            JsonObject jsonObject = format.GetObject();
            ImageFormat result = new ImageFormat();
            result.JsonFormat = jsonObject;
            result.Format = jsonObject.GetNamedString("Subtype");
            result.Height = (int)jsonObject.GetNamedNumber("Height");
            result.Width = (int)jsonObject.GetNamedNumber("Width");
            return result;
        }
    }
}
