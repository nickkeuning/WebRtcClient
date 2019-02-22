using Org.WebRtc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebRtcClient.Signalling;
using WebRtcClient.Utilities;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace WebRtcClient
{
    class Client
    {
        private string ServerAddress { get;  }
        private MediaElement RemoteVideo { get;  }
        private CoreDispatcher CoreDispatcher { get; }

        private RtcWrapper Wrapper { get; }

        private List<RTCIceCandidate> IceCandidates { get; set; } = new List<RTCIceCandidate>();
        private RTCPeerConnection PeerConnection { get; set; }


        public Client(CoreDispatcher coreDispatcher, MediaElement remoteVideo, string serverAddress)
        {
            this.ServerAddress = serverAddress;
            this.CoreDispatcher = coreDispatcher;
            this.RemoteVideo = remoteVideo;

            this.Wrapper = RtcWrapper.Instance;
        }

        public async Task Initialize()
        {
            await this.Wrapper.Initialize(this.CoreDispatcher);
        }


        public async Task ConnectToServer()
        {
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

            // Add local stream
            this.PeerConnection.AddStream(this.Wrapper.MediaStream);

            // Connection protocol
            await this.WebRtcHandshake();
            await this.SubmitIceCandidatesAsync();

            // Shut off local video since this is the client
            foreach (var track in this.Wrapper.MediaStream.GetTracks())
            {
                this.Wrapper.MediaStream.RemoveTrack(track);
            }
        }

        private async Task WebRtcHandshake()
        {
            var localDescription = await this.PeerConnection.CreateOffer();
            await this.PeerConnection.SetLocalDescription(localDescription);

            var remoteDescription = await SignallingClient.SendOfferAndGetAnswerAsync(localDescription, this.ServerAddress);
            await this.PeerConnection.SetRemoteDescription(remoteDescription);
        }

        private async Task SubmitIceCandidatesAsync()
        {
            var Complete = RTCIceGatheringState.Complete;
            await Task.Run(() => SpinWait.SpinUntil(() => this.PeerConnection.IceGatheringState == Complete));
            foreach (var candidate in this.IceCandidates)
            {
                var answer = await SignallingClient.SendIceCandidateAndGetResonse(candidate, this.ServerAddress);
                Logger.Log($"Submitted ice candidate, received answer: \n{answer}");
            }
        }

        private void PeerConnection_OnAddStream(MediaStreamEvent evt)
        {
            var remoteVideoTrack = evt.Stream.GetVideoTracks().FirstOrDefault();
            if (remoteVideoTrack != null)
            {
                this.Wrapper.Media.AddVideoTrackMediaElementPair(remoteVideoTrack, this.RemoteVideo, "Remote");
            }
        }

        private void PeerConnection_OnIceCandidate(RTCPeerConnectionIceEvent iceEvent)
        {
            Logger.Log("On ice candidate triggered");
            this.IceCandidates.Add(iceEvent.Candidate);
        }
    }
}
