namespace Tuku.Contracts.Cad
{
    using System.Collections.Generic;

    public enum CadInsertPhase
    {
        Received = 0,
        WaitingForPoint = 1,
        Executing = 2,
        Succeeded = 3,
        Cancelled = 4,
        Failed = 5,
        Unknown = 6
    }

    public enum CadTextItemKind
    {
        Name = 0,
        Code = 1,
        Layer = 2,
        Notes = 3,
        Reference = 4
    }

    public sealed class CadHelloMessage
    {
        public string PluginVersion { get; set; }

        public string CadVersion { get; set; }

        public int ProcessId { get; set; }

        public string SessionId { get; set; }

        public List<string> Capabilities { get; set; }
    }

    public sealed class CadDocumentInfo
    {
        public string DocumentId { get; set; }

        public string DisplayName { get; set; }

        public bool IsActive { get; set; }

        public bool HasUnsavedChanges { get; set; }
    }

    public sealed class CadListDocumentsRequest
    {
    }

    public sealed class CadListDocumentsResponse
    {
        public List<CadDocumentInfo> Documents { get; set; }
    }

    public sealed class CadTextItem
    {
        public CadTextItemKind Kind { get; set; }

        public string Text { get; set; }

        public int Order { get; set; }
    }

    public sealed class CadInsertStyle
    {
        public string TextStyleName { get; set; }

        public double TextHeight { get; set; }

        public double ContentWidth { get; set; }

        public double ParagraphSpacing { get; set; }

        public string LayerName { get; set; }
    }

    public sealed class CadInsertPracticeRequest
    {
        public string TargetInstance { get; set; }

        public string TargetDocumentId { get; set; }

        public string PracticeId { get; set; }

        public int PracticeRevision { get; set; }

        public List<CadTextItem> Items { get; set; }

        public CadInsertStyle Style { get; set; }
    }

    public sealed class CadInsertStatus
    {
        public string RequestId { get; set; }

        public CadInsertPhase Phase { get; set; }

        public string Message { get; set; }

        public int? CreatedObjectCount { get; set; }
    }

    public sealed class CadGetRequestStatus
    {
        public string RequestId { get; set; }
    }

    public sealed class CadCancelRequest
    {
        public string RequestId { get; set; }
    }

    public sealed class CadEndpointRegistration
    {
        public int ProcessId { get; set; }

        public string CadVersion { get; set; }

        public string PluginVersion { get; set; }

        public string SessionId { get; set; }

        public string PipeName { get; set; }

        public long RegisteredUtcTicks { get; set; }
    }
}
