namespace Tuku.Contracts.Recognition
{
    public enum RecognitionInputKind
    {
        Text = 0,
        Image = 1
    }

    public enum RecognitionOutcomeStatus
    {
        Succeeded = 0,
        Failed = 1,
        Unknown = 2
    }

    public enum PageContentKind
    {
        Unknown = 0,
        HasPractices = 1,
        NoPracticeContent = 2,
        NeedsAttention = 3
    }

    public sealed class RecognitionTask
    {
        public string InputFingerprint { get; set; }

        public string ConfigFingerprint { get; set; }

        public RecognitionInputKind InputKind { get; set; }

        public string Provider { get; set; }

        public string Model { get; set; }

        public string Endpoint { get; set; }

        public int? PageNumber { get; set; }

        public string ImageCacheKey { get; set; }
    }
}
