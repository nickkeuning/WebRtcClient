using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage.Streams;


namespace WebRtcClient.Signalling
{
    abstract class Signaller
    {
        protected static class Protocol
        {
            public static string DefaultPort { get; } = "8888";
            public static string MessageTypeKeyName { get; } = "MessageType";
            public static string MessageContentsKeyName { get; } = "MessageContents";


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

            public static JsonObject parseRawMessage(string rawMessageJson)
            {
                if (!JsonObject.TryParse(rawMessageJson, out JsonObject messageJsonObject))
                {
                    throw new Exception("Unable to parse answer from server into valid json.");
                }
                return messageJsonObject;
            }

            public static MessageType GetMessageType(JsonObject messageJsonObject)
            {
                if (!messageJsonObject.TryGetValue("MessageType", out IJsonValue messageTypeJsonObject))
                {
                    throw new Exception("Unable to find message type in server message.");
                }
                return JsonConvert.DeserializeObject<MessageType>(messageTypeJsonObject.GetString());
            }

            public static string GetMessageContents(JsonObject messageJsonObject)
            {
                if (!messageJsonObject.TryGetValue("MessageContents", out IJsonValue messageContentsJsonValue))
                {
                    throw new Exception("Unable to find message contents in server message.");
                }
                return messageContentsJsonValue.GetString();
            }

            public enum MessageType
            {
                Offer,
                Answer,
                IceCandidate,
                IceAnswer,
                Shutdown,
                ShutdownAck
            }

            public static string WrapMessage(string message, MessageType messageType)
            {
                var messageTypeSerialized = JsonConvert.SerializeObject(messageType);
                JsonObject keyValuePairs = new JsonObject()
            {
                { Protocol.MessageTypeKeyName, JsonValue.CreateStringValue(messageTypeSerialized) },
                { Protocol.MessageContentsKeyName, JsonValue.CreateStringValue(message) }
            };
                return keyValuePairs.Stringify();
            }

        }        
    }

}
