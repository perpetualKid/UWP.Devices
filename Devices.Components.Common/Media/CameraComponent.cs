using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Devices.Util.Extensions;
using Windows.Data.Json;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace Devices.Components.Common.Media
{
    public class CameraComponent : ComponentBase
    {
        private MediaCapture mediaCapture;
        private MediaCaptureInitializationSettings mediaCaptureSettings;
        private IEnumerable<IMediaEncodingProperties> supportedFormats;

        public CameraComponent(string componentName, MediaCapture mediaCapture) : base(componentName)
        {
            this.mediaCapture = mediaCapture;
        }

        public CameraComponent(string componentName, MediaCaptureInitializationSettings mediaCaptureSettings) : base(componentName)
        {
            this.mediaCaptureSettings = mediaCaptureSettings;

        }

        protected override async Task InitializeDefaults()
        {
            if (null == mediaCapture)
                mediaCapture = new MediaCapture();
            if (null != mediaCaptureSettings)
            {
                await mediaCapture.InitializeAsync(mediaCaptureSettings).AsTask().ConfigureAwait(false);
            }
            else
            {
                await mediaCapture.InitializeAsync().AsTask().ConfigureAwait(false);
            }
            await base.InitializeDefaults();
        }

        #region command handling
        [Action("Capture")]
        [ActionHelp("Takes a picture and returns as Base64 string.")]
        private async Task CameraComponentCapture(MessageContainer data)
        {
            string imageBase64 = string.Empty;
            using (IRandomAccessStream stream = await CaptureMediaStream(ImageEncodingProperties.CreateJpeg()).ConfigureAwait(false))
            {
                byte[] bytes = new byte[stream.Size];
                using (DataReader reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(bytes);
                }
                imageBase64 = Convert.ToBase64String(bytes);
            }
            data.AddValue("ImageBase64", imageBase64);
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
        }

        [Action("GetCurrentFormat")]
        [Action("GetFormat")]
        [ActionHelp("Returns current capture format.")]
        private async Task CameraComponentGetCurrentFormat(MessageContainer data)
        {
            data.AddMultiPartValue("MediaFormat", MediaPropertiesToJson(await GetCurrentFormat()));
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
        }

        [Action("SetCurrentFormat")]
        [Action("SetFormat")]
        [ActionParameter("Width")]
        [ActionParameter("Height")]
        [ActionHelp("Sets the current capture format")]
        private async Task CameraComponentSetCurrentFormat(MessageContainer data)
        {
            await Task.CompletedTask;
            uint width = uint.Parse(data.ResolveParameter("Width", 0));
            uint height = uint.Parse(data.ResolveParameter("Height", 1));
            //VideoEncodingProperties mediaFormat = VideoEncodingProperties.CreateUncompressed("YUY2", width, height);
            VideoEncodingProperties mediaFormat = VideoEncodingProperties.CreateMpeg2();
            mediaFormat.Width = width;
            mediaFormat.Height = height;
            await SetCurrentFormat(mediaFormat).ConfigureAwait(false);
        }

        [Action("ListFormats")]
        [Action("GetAllFormats")]
        [ActionParameter("Type", Required = false)]
        [ActionParameter("SubType", Required = false)]
        [ActionParameter("Width", Required = false)]
        [ActionParameter("Height", Required = false)]
        [ActionParameter("BitRate", Required = false)]
        [ActionHelp("Returns a list of available capture formats. If provided, filtered by the given parameters")]
        private async Task CameraComponentGetAllFormats(MessageContainer data)
        {
            uint width;
            uint height;
            uint bitrate;
            string type = data.ResolveParameter("Type", 0);
            string subtype = data.ResolveParameter("Subtype", 1);
            if (!uint.TryParse(data.ResolveParameter("Width", 2), out width))
                width = 0;
            if (!uint.TryParse(data.ResolveParameter("Height", 3), out height))
                height = 0;
            if (!uint.TryParse(data.ResolveParameter("BitRate", 4), out bitrate))
                bitrate = 0;

            foreach (var item in await GetSupportedMediaFormats(type, subtype, width, height, bitrate).ConfigureAwait(false))
            {
                data.AddMultiPartValue("MediaFormat", MediaPropertiesToJson(item));
            }
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
        }
        #endregion

        #region public
        public async Task<IRandomAccessStream> CaptureMediaStream(ImageEncodingProperties encoding)
        {
            InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
            ImageEncodingProperties imageProperties = encoding;
            await mediaCapture.CapturePhotoToStreamAsync(imageProperties, stream).AsTask().ConfigureAwait(false);
            stream.Seek(0);
            return stream;
        }

        public async Task<IEnumerable<IMediaEncodingProperties>> GetSupportedMediaFormats()
        {
            if (null != supportedFormats)
                return await Task.FromResult<IEnumerable<IMediaEncodingProperties>>(supportedFormats).ConfigureAwait(false);
            return await Task.Run(() =>
            {
                supportedFormats = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo)
                    .OrderBy(type => type.Type)
                    .ThenBy(subType => subType.Subtype)
                    .ThenByDescending(resolution =>
                    {
                        return (resolution is VideoEncodingProperties ? ((VideoEncodingProperties)resolution).Width * ((VideoEncodingProperties)resolution).Height :
                            resolution is ImageEncodingProperties ? ((ImageEncodingProperties)resolution).Width * ((ImageEncodingProperties)resolution).Height :
                            (uint)0);
                    })
                    .ThenByDescending(frameRate =>
                    {
                        return (frameRate is VideoEncodingProperties ? 
                            ((VideoEncodingProperties)frameRate).FrameRate.Numerator / ((VideoEncodingProperties)frameRate).FrameRate.Denominator : 0.0);
                    });
                return supportedFormats;
            }).ConfigureAwait(false);
        }

        public async Task<VideoEncodingProperties> GetCurrentFormat()
        {
            return await Task.Run(() => mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.Photo)).ConfigureAwait(false) as VideoEncodingProperties;
        }

        public async Task SetCurrentFormat(VideoEncodingProperties format)
        {
            await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, format).AsTask().ConfigureAwait(false);
        }

        public async Task<IEnumerable<IMediaEncodingProperties>> GetSupportedMediaFormats(string type, string subType, uint width, uint height, uint bitrate)
        {
            IEnumerable<IMediaEncodingProperties> formats = await GetSupportedMediaFormats().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(type))
                formats = formats.Where(format => (format).Type.ToLowerInvariant() == type.ToLowerInvariant());
            if (!string.IsNullOrWhiteSpace(subType))
                formats = formats.Where(format => (format).Subtype.ToLowerInvariant() == subType.ToLowerInvariant());
            if (width > 0)
                formats = formats.Where(format =>
                {
                    return (format is VideoEncodingProperties ? ((VideoEncodingProperties)format).Width <= width :
                        (format is ImageEncodingProperties ? ((ImageEncodingProperties)format).Width <= width : false));
                });
            if (height > 0)
                formats = formats.Where(format =>
            {
                return (format is VideoEncodingProperties ? ((VideoEncodingProperties)format).Height <= height :
                    (format is ImageEncodingProperties ? ((ImageEncodingProperties)format).Height <= height: false));
            });
            if (bitrate> 0)
                formats = formats.Where(format =>
                {
                return (format is VideoEncodingProperties ? ((VideoEncodingProperties)format).Bitrate <= bitrate : false);
                });
            return formats;
        }

        #endregion

        #region properties
        public MediaCapture MediaCapture { get { return this.mediaCapture; } }

        #endregion

        #region private helpers
        private JsonObject MediaPropertiesToJson(IMediaEncodingProperties mediaProperties)
        {
            JsonObject properties = new JsonObject();
            if (mediaProperties is VideoEncodingProperties)
            {
                VideoEncodingProperties videoProperties = mediaProperties as VideoEncodingProperties;
                properties.AddValue(nameof(videoProperties.Bitrate), videoProperties.Bitrate);
                properties.AddValue(nameof(videoProperties.FrameRate), $"{videoProperties.FrameRate.Denominator}/{videoProperties.FrameRate.Numerator}");
                properties.AddValue(nameof(videoProperties.Height), videoProperties.Height);
                properties.AddValue(nameof(videoProperties.ProfileId), videoProperties.ProfileId);
                properties.AddValue(nameof(videoProperties.PixelAspectRatio), $"{videoProperties.PixelAspectRatio.Denominator}/{videoProperties.PixelAspectRatio.Numerator}");
                properties.AddValue(nameof(videoProperties.Subtype), videoProperties.Subtype);
                properties.AddValue(nameof(videoProperties.Type), videoProperties.Type);
                properties.AddValue(nameof(videoProperties.Width), videoProperties.Width);
            }
            else if (mediaProperties is ImageEncodingProperties)
            {
                ImageEncodingProperties imageProperties = mediaProperties as ImageEncodingProperties;
                properties.AddValue(nameof(imageProperties.Height), imageProperties.Height);
                properties.AddValue(nameof(imageProperties.Subtype), imageProperties.Subtype);
                properties.AddValue(nameof(imageProperties.Type), imageProperties.Type);
                properties.AddValue(nameof(imageProperties.Width), imageProperties.Width);
            }
            return properties;
        }
        #endregion
    }
}
