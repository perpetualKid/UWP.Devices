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
                return ToLongString();
            }

            public string ToShortString()
            {
                return $"{Width}*{Height}, {Format}";
            }

            public string ToLongString()
            {
                return $"Resolution:{Width}*{Height}, CaptureFormat:{Format}, BitRate:{JsonFormat.GetNamedNumber("Bitrate")}, FrameRate:{JsonFormat.GetNamedString("FrameRate")}, PixelAspectRatio:{JsonFormat.GetNamedString("PixelAspectRatio")}";
            }

            public override bool Equals(object obj)
            {
                return (obj is ImageFormat) ? (this.JsonFormat.Stringify() == ((ImageFormat)obj).JsonFormat.Stringify()) : false;
            }
        }

        private ObservableCollection<BitmapImage> images;
        private ImageFormat? currentImageFormat;
        private BitmapImage currentImage;

        private List<ImageFormat> supportedImageFormats;

        public event EventHandler<BitmapImage> OnImageReceived;

        public event EventHandler<ImageFormat> OnCurrentFormatChanged;

        public event EventHandler OnSupportedFormatsChanged;

        public ImageSourceController(string name, string cameraName): base(name, cameraName)
        {
            images = new ObservableCollection<BitmapImage>();
            supportedImageFormats = new List<ImageFormat>();
        }

        public int ImageCacheSize { get; set; } = 10;

        public ObservableCollection<BitmapImage> CachedImages { get { return this.images; } }

        public List<ImageFormat> SupportedImageFormats { get { return this.supportedImageFormats; } }

        public ImageFormat? CurrentImageFormat { get { return this.currentImageFormat; } }

        public BitmapImage CurrentImage { get { return this.currentImage; } }

        public IEnumerable<string> GetSupportedCaptureFormats(string resolution)
        {
            return supportedImageFormats?.
                Where(format => (string.IsNullOrEmpty(resolution) ? true : $"{format.Width}*{format.Height}" == resolution)).
                Select((formatJson) => formatJson.Format).
                Distinct() ?? new string[0];
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

        public IEnumerable<ImageFormat> GetSupportedFormatsFiltered(string subType, string resolution)
        {
            return supportedImageFormats?.
                Where(format => (string.IsNullOrEmpty(subType) ? true : format.Format == subType)).
                Where(format => (string.IsNullOrEmpty(resolution) ? true : $"{format.Width}*{format.Height}" == resolution)) ?? new List<ImageFormat>();
        }

        public ImageFormat? SelectFormat(string subType, string resolution, string bitrate)
        {
            return supportedImageFormats?.
                Where(format => (string.IsNullOrEmpty(subType) ? true : format.Format == subType)).
                Where(format => (string.IsNullOrEmpty(resolution) ? true : $"{format.Width}*{format.Height}" == resolution)).
                Where(format => (string.IsNullOrEmpty(bitrate) ? true : format.JsonFormat.GetNamedNumber("Bitrate").ToString() == bitrate)).
                FirstOrDefault();
        }

        public async Task CaptureDeviceImage()
        {
            JsonObject imageCapture = new JsonObject();
            imageCapture.AddValue("Action", "Capture");
            await ControllerHandler.Send(this, imageCapture).ConfigureAwait(false);

        }

        public async Task RequestSupportedFormats(string type = null, string subType = null)
        {
            JsonObject imageCapture = new JsonObject();
            imageCapture.AddValue("Action", "ListFormats");
            if (!string.IsNullOrEmpty(type))
                imageCapture.AddValue("Type", type);
            if (!string.IsNullOrEmpty(subType))
                imageCapture.AddValue("SubType", subType);

            await ControllerHandler.Send(this, imageCapture).ConfigureAwait(false);
        }

        public async Task RequestCurrentFormat()
        {
            JsonObject imageCapture = new JsonObject();
            imageCapture.AddValue("Action", "GetCurrentFormat");
            await ControllerHandler.Send(this, imageCapture).ConfigureAwait(false);
        }

        public async Task SetCaptureFormat(ImageFormat imageFormat)
        {
            JsonObject imageFormatJson = new JsonObject();
            imageFormatJson.AddValue("Action", "SetCurrentFormat");
            imageFormatJson.AddValue("SubType", imageFormat.Format);
            imageFormatJson.AddValue("Width", imageFormat.Width);
            imageFormatJson.AddValue("Height", imageFormat.Height);
            imageFormatJson.AddValue("BitRate", imageFormat.JsonFormat.GetNamedNumber("Bitrate"));

            await ControllerHandler.Send(this, imageFormatJson).ConfigureAwait(false);
            currentImageFormat = imageFormat;
        }

        [TargetAction("GetCurrentFormat")]
        private Task GetCurrentFormatResponse(JsonObject data)
        {
            currentImageFormat = JsonFormatToImageFormat(data.GetNamedValue("MediaFormat"));
            OnCurrentFormatChanged?.Invoke(this, currentImageFormat.Value);
            return Task.CompletedTask;
        }

        [TargetAction("ListFormats")]
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

        [TargetAction("Capture")]
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
