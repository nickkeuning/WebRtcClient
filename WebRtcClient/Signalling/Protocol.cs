using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Storage.Streams;
using Windows.Data.Json;

namespace WebRtcClient.Signalling
{
    static class Protocol
    {
        private static string DefaultPort { get; } = "8888";
        private static string DefaultAddress { get; } = "localhost";
        private static string MessageTypeKeyName { get; } = "MessageType";
        private static string MessageContentsKeyName { get; } = "MessageContents";


        public static async Task SendMessageToStreamAsync(IOutputStream outputStream, string message)
        {
            using (var writer = new DataWriter(outputStream))
            {
                writer.WriteUInt32(writer.MeasureString(message));

                writer.WriteString(message);
                await writer.StoreAsync();
            }
        }

        public static async Task<string> ReadMessageFromStreamAsync(IInputStream inputStream)
        {
            using (var reader = new DataReader(inputStream))
            {
                await reader.LoadAsync(sizeof(uint));
                var numBytes = reader.ReadUInt32();

                await reader.LoadAsync(numBytes);
                return reader.ReadString(numBytes);
            }
        }

        public static async Task<string> SendMessageAndGetResponseAsync(string message, string address, string port)
        {
            string response;
            using (var streamSocket = new StreamSocket())
            {
                var hostname = new HostName(address);

                await streamSocket.ConnectAsync(hostname, DefaultPort);

                using (var outputStream = streamSocket.OutputStream)
                {
                    await Protocol.SendMessageToStreamAsync(outputStream, message);
                }

                using (var inputStream = streamSocket.InputStream)
                {
                    response = await Protocol.ReadMessageFromStreamAsync(inputStream);
                }
            }

            return response;
        }

        public static async Task<string> SendMessageAndGetResponseAsync(string message, string address)
        {
            return await Protocol.SendMessageAndGetResponseAsync(message, address, Protocol.DefaultPort);
        }

        public static string WrapMessage(this string message, MessageType messageType)
        {
            JsonObject keyValuePairs = new JsonObject()
            {
                { Protocol.MessageTypeKeyName, JsonValue.CreateStringValue(messageType.Name()) },
                { Protocol.MessageContentsKeyName, JsonValue.CreateStringValue(message) }
            };
            return keyValuePairs.Stringify();
        }

        public enum MessageType
        {
            Offer,
            Answer,
            IceCandidate
        }

        public static string Name(this MessageType messageType)
        {
            return Enum.GetName(typeof(MessageType), messageType);
        }


    }
}
