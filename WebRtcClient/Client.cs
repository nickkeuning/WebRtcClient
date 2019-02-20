using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.WebRtc;
using Windows.UI.Xaml.Controls;
using Windows.UI.Core;
using Newtonsoft.Json;
using WebRtcClient.Utilities;
using WebRtcClient.Signalling;
using Windows.Data.Json;
using System.Threading;

namespace WebRtcClient
{
    class Client
    {
        private string ServerAddress { get; set; }

        private List<RTCIceCandidate> IceCandidates { get; set; } = new List<RTCIceCandidate>();
        private RTCPeerConnection PeerConnection { get; set; }
        private Media Media { get; set; }
        private MediaStream MediaStream { get; set; }

        private MediaElement RemoteVideo { get; set; }
        private CoreDispatcher CoreDispatcher { get; set; }


        public Client(string serverAddress, CoreDispatcher coreDispatcher, MediaElement remoteVideo)
        {
            this.ServerAddress = serverAddress;
            this.CoreDispatcher = coreDispatcher;
            this.RemoteVideo = remoteVideo;
        }

        public async Task ConnectToServer()
        {
            // Initialize WebRtc
            bool allowed = await WebRTC.RequestAccessForMediaCapture();
            if (allowed)
            {
                WebRTC.Initialize(this.CoreDispatcher);
            }
            else
            {
                throw new System.Exception("Failed to access needed media");
            }

            // Initialize PeerConnection
            this.PeerConnection = new RTCPeerConnection(
                new RTCConfiguration()
                {
                    BundlePolicy = RTCBundlePolicy.Balanced,
                    IceTransportPolicy = RTCIceTransportPolicy.All
                }
            );

            this.PeerConnection.OnIceCandidate += PeerConnection_OnIceCandidate;
            this.PeerConnection.OnAddStream += PeerConnection_OnAddStream;

            // Start media capture and add to stream
            this.Media = Media.CreateMedia();

            var videoDevice = this.Media.GetVideoCaptureDevices().First();
            var capabilities = await videoDevice.GetVideoCaptureCapabilities();

            var lowestBitRate = capabilities
                .OrderBy(cap => cap.Width * cap.Height * cap.FrameRate)
                .FirstOrDefault();

            if (lowestBitRate != null)
            {
                WebRTC.SetPreferredVideoCaptureFormat(
                    (int)lowestBitRate.Width,
                    (int)lowestBitRate.Height,
                    (int)lowestBitRate.FrameRate,
                    lowestBitRate.MrcEnabled
                );
            }

            var constraints = new RTCMediaStreamConstraints()
            {
                audioEnabled = false,
                videoEnabled = true
            };

            this.MediaStream = await this.Media.GetUserMedia(constraints);            
            this.PeerConnection.AddStream(this.MediaStream);

            // Connection protocol
            await this.WebRtcHandshake();

            Logger.Log("Completed WebRtc handshake...");
            Logger.Log("Submitting ice candidates to server");
            await this.SubmitIceCandidatesAsync();

            foreach (var track in this.MediaStream.GetTracks())
            {
                this.MediaStream.RemoveTrack(track);
            }
            Logger.Log($"Medeia stream is active? {this.MediaStream.Active.ToString()}");


            Logger.Log("Done connecting...");
        }

        private async Task SubmitIceCandidatesAsync()
        {
            var Complete = RTCIceGatheringState.Complete;
            await Task.Run(() => SpinWait.SpinUntil(() => this.PeerConnection.IceGatheringState == Complete));
            foreach (var candidate in this.IceCandidates)
            {
                var message = JsonConvert.SerializeObject(candidate).WrapMessage(Protocol.MessageType.IceCandidate);
                var answer = await Protocol.SendMessageAndGetResponseAsync(message, this.ServerAddress);
                Logger.Log($"Submitted ice candidate, received answer: \n{answer}");
            }
        }

        private async Task WebRtcHandshake()
        {
            var offer = await this.PeerConnection.CreateOffer();

            await this.PeerConnection.SetLocalDescription(offer);

            var message = JsonConvert.SerializeObject(offer).WrapMessage(Protocol.MessageType.Offer);

            var answer = await Protocol.SendMessageAndGetResponseAsync(message, this.ServerAddress);

            var res = await this.DispatchMessageFromServer(answer);
        }


        private async Task<string> DispatchMessageFromServer(string rawServerMessageJson)
        {
            var dispatch = new Dictionary<string, Func<string, Task<string>>>()
            {
                { Protocol.MessageType.Answer.Name(), this.HandleServerAnswer }
            };

            if (JsonObject.TryParse(rawServerMessageJson, out JsonObject result))
            {
                var messageType = result["MessageType"].GetString();
                if (dispatch.ContainsKey(messageType))
                {
                    var messageContents = result["MessageContents"].GetString();
                    return await dispatch[messageType](messageContents);
                }
                else
                {
                    throw new System.Exception("Message from client contains invalid message type...");
                }
            }
            else
            {
                throw new System.Exception("Unable to parse message from client into json object...");
            }
        }

        private async Task<string> HandleServerAnswer(string serverMessageContentsJson)
        {
            var res = JsonConvert.DeserializeObject<RTCSessionDescription>(serverMessageContentsJson);
            await this.PeerConnection.SetRemoteDescription(res);
            return $"Received answer from server: \n{serverMessageContentsJson}";
        }

        private void PeerConnection_OnAddStream(MediaStreamEvent evt)
        {
            var remoteVideoTrack = evt.Stream.GetVideoTracks().FirstOrDefault();
            if (remoteVideoTrack != null)
            {
                Media.AddVideoTrackMediaElementPair(remoteVideoTrack, this.RemoteVideo, "Remote");
            }
        }

        private void PeerConnection_OnIceCandidate(RTCPeerConnectionIceEvent iceEvent)
        {
            Logger.Log("On ice candidate triggered");
            this.IceCandidates.Add(iceEvent.Candidate);
        }


    }
}
