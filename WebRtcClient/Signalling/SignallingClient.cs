using Newtonsoft.Json;
using Org.WebRtc;
using System;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using WebRtcClient.Utilities;

namespace WebRtcClient.Signalling
{
    class SignallingClient : Signaller
    {
        public static async Task<RTCSessionDescription> SendOfferAndGetAnswerAsync(RTCSessionDescription sessionDescription, string address)
        {
            var objectJson = JsonConvert.SerializeObject(sessionDescription);
            var serverAnswer = await SendMessageAndGetResponseAsync(objectJson, Protocol.MessageType.Offer, Protocol.MessageType.Answer, address);
            return JsonConvert.DeserializeObject<RTCSessionDescription>(serverAnswer);
        }

        public static async Task<string> SendIceCandidateAndGetResonse(RTCIceCandidate iceCandidate, string address)
        {
            var objectJson = JsonConvert.SerializeObject(iceCandidate);
            var serverAnswer = await SendMessageAndGetResponseAsync(objectJson, Protocol.MessageType.IceCandidate, Protocol.MessageType.IceAnswer, address);
            return serverAnswer;
        }

        private static async Task<string> SendMessageAndGetResponseAsync(string message, Protocol.MessageType messageType, Protocol.MessageType answerType, string address)
        {
            var wrappedMessage = Protocol.WrapMessage(message, messageType);

            string response;
            using (var streamSocket = new StreamSocket())
            {
                var hostname = new HostName(address);

                Logger.Log("Attempting to connect to server...");
                await streamSocket.ConnectAsync(hostname, Protocol.DefaultPort);
                Logger.Log("Sending message...");
                await Protocol.SendMessageToStreamAsync(streamSocket.OutputStream, wrappedMessage);
                Logger.Log("Reading response...");
                response = await Protocol.ReadMessageFromStreamAsync(streamSocket.InputStream);
            }

            var serverAnswerJson = Protocol.parseRawMessage(response);
            if (Protocol.GetMessageType(serverAnswerJson) != answerType)
            {
                throw new Exception("Server did not respond with correct answer type for client message.");
            }
            return Protocol.GetMessageContents(serverAnswerJson);
        }
    }
}
