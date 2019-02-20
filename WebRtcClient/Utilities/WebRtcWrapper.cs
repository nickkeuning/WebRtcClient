using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.WebRtc;

namespace WebRtcClient.Utilities
{
    static class WebRtcWrapper
    {
        //public static Media Media { get; private set; }

        static WebRtcWrapper()
        {
            var coreDispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;

            Task.Run(() => coreDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                WebRTC.RequestAccessForMediaCapture().AsTask().ContinueWith(allowed =>
                {
                    if (allowed.Result)
                    {
                        WebRTC.Initialize(coreDispatcher);
                    }
                    else
                    {
                        throw new System.Exception("Unable to access camera and microphone.");
                    }
                });
            })).Wait();
            

            Logger.Log("WebRtcInit: Got access to local media...");


            WebRTC.EnableLogging(LogLevel.LOGLVL_VERBOSE);

            Logger.Log("WebRtcInit: Set log level to verbose...");

            //Media = MediaWrapper.Instance;
        }

        public static void Initialize() { return; }


        private sealed class MediaWrapper
        {
            private static volatile Media instance;
            private static object mediaLock = new Object();

            public static Media Instance
            {
                get
                {
                    if (instance == null)
                    {
                        lock (mediaLock)
                        {
                            if (instance == null)
                                instance = Media.CreateMedia();
                        }
                    }

                    return instance;
                }
            }
        }
    }
}
