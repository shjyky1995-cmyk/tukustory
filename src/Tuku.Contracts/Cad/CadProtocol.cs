namespace Tuku.Contracts.Cad
{
    using Newtonsoft.Json.Linq;

    public static class CadProtocol
    {
        public const int CurrentVersion = 1;

        public const int MinSupportedVersion = 1;

        public const string MessageHello = "hello";
        public const string MessageCapabilities = "capabilities";
        public const string MessageListDocuments = "list_documents";
        public const string MessageDocuments = "documents";
        public const string MessageInsertPractice = "insert_practice";
        public const string MessageInsertStatus = "insert_status";
        public const string MessageGetRequestStatus = "get_request_status";
        public const string MessageCancelRequest = "cancel_request";

        public const int MaxMessageBytes = 4 * 1024 * 1024;

        public static bool IsSupportedVersion(int version)
        {
            return version >= MinSupportedVersion && version <= CurrentVersion;
        }
    }

    public sealed class ProtocolMessage
    {
        public int ProtocolVersion { get; set; }

        public string RequestId { get; set; }

        public string Type { get; set; }

        public JObject Payload { get; set; }

        public static ProtocolMessage Create<TPayload>(string type, string requestId, TPayload payload)
        {
            return new ProtocolMessage
            {
                ProtocolVersion = CadProtocol.CurrentVersion,
                RequestId = requestId,
                Type = type,
                Payload = payload == null ? new JObject() : JObject.FromObject(payload)
            };
        }

        public static ProtocolMessage CreateEmpty(string type, string requestId)
        {
            return new ProtocolMessage
            {
                ProtocolVersion = CadProtocol.CurrentVersion,
                RequestId = requestId,
                Type = type,
                Payload = new JObject()
            };
        }

        public TPayload ReadPayload<TPayload>()
        {
            if (Payload == null)
            {
                return default(TPayload);
            }

            return Payload.ToObject<TPayload>();
        }
    }
}
