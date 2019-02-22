using Org.WebRtc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace WebRtcClient.Utilities
{
    sealed class RtcWrapper
    {
        private Media media { get; set; }
        private MediaStream mediaStream { get; set; }

        private RTCMediaStreamConstraints Constraints { get; } = new RTCMediaStreamConstraints()
        {
            videoEnabled = true,
            audioEnabled = false
        };

        private RtcWrapper()
        {
            this.media = null;
            this.mediaStream = null;
        }

        public Media Media { get { return Nested.instance.media; } }
        public MediaStream MediaStream { get { return Nested.instance.mediaStream; } }
        public static RtcWrapper Instance { get { return Nested.instance; } }

        public async Task Initialize(CoreDispatcher coreDispatcher)
        {
            if (this.media != null || this.mediaStream != null)
            {
                throw new Exception("Media lock is alreay initialized.");
            }

            var allowed = await WebRTC.RequestAccessForMediaCapture();
            if (!allowed)
            {
                throw new Exception("Failed to access media for WebRtc...");
            }

            WebRTC.Initialize(coreDispatcher);

            this.media = Media.CreateMedia();

            var videoDevice = this.media.GetVideoCaptureDevices().First();
            var capabilities = await videoDevice.GetVideoCaptureCapabilities();

            var selectedFormat = capabilities
                .OrderBy(cap => cap.Width * cap.Height * cap.FrameRate)
                .FirstOrDefault();

            if (selectedFormat != null)
            {
                WebRTC.SetPreferredVideoCaptureFormat(
                    (int)selectedFormat.Width,
                    (int)selectedFormat.Height,
                    (int)selectedFormat.FrameRate,
                    selectedFormat.MrcEnabled
                );
            }

            this.mediaStream = await this.media.GetUserMedia(this.Constraints);
        }

        private class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
            }
            internal static readonly RtcWrapper instance = new RtcWrapper();
        }
    }

}
