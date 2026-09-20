namespace Tuku.Contracts.Recognition
{
    using System.Collections.Generic;

    public sealed class ExtractedLayer
    {
        public string OriginalText { get; set; }

        public string CurrentText { get; set; }
    }

    public sealed class ExtractedSource
    {
        public int? PageNumber { get; set; }

        public double? X { get; set; }

        public double? Y { get; set; }

        public double? Width { get; set; }

        public double? Height { get; set; }

        public string RegionSource { get; set; }

        public bool RegionReliable { get; set; }
    }

    public sealed class ExtractedPracticeCandidate
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public string Notes { get; set; }

        public string ReferenceNote { get; set; }

        public string SuggestedPartName { get; set; }

        public List<string> SuggestedTags { get; set; }

        public List<ExtractedLayer> Layers { get; set; }

        public List<ExtractedSource> Sources { get; set; }

        public List<string> Problems { get; set; }

        public double? Confidence { get; set; }
    }

    public sealed class PageExtractionResult
    {
        public int? PageNumber { get; set; }

        public PageContentKind ContentKind { get; set; }

        public List<ExtractedPracticeCandidate> Candidates { get; set; }

        public List<string> Problems { get; set; }
    }
}
