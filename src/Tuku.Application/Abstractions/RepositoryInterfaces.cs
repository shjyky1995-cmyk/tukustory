namespace Tuku.Application.Abstractions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Tuku.Application.Dtos;
    using Tuku.Contracts.Recognition;
    using Tuku.Domain.Taxonomy;

    public interface IPracticeRepository
    {
        PagedResult<PracticeListItem> Query(PracticeQuery query);

        PracticeDetail GetDetail(Guid practiceId);

        IReadOnlyList<RevisionSummary> GetRevisions(Guid practiceId);

        PracticeSaveResult Save(PracticeEditCommand command);

        PracticeSaveResult RestoreRevision(Guid practiceId, int revisionNumber, int expectedRevision, string reason);

        PracticeSaveResult SetVerified(Guid practiceId, bool verified);
    }

    public interface IAtlasRepository
    {
        Guid CreateAtlas(string name, string code, Guid regionId, string yearOrVersionNote, string originalFileFingerprint, string managedFilePath);

        AtlasSummary GetAtlas(Guid atlasId);

        IReadOnlyList<AtlasSummary> ListAtlases(bool includeArchived);

        void RegisterPage(Guid atlasId, int pageNumber, string printedPageLabel, double widthPoints, double heightPoints, string imageCacheKey);

        IReadOnlyList<AtlasPageSummary> GetPages(Guid atlasId);

        void ArchiveAtlas(Guid atlasId);
    }

    public sealed class AtlasSummary
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public Guid RegionId { get; set; }

        public string RegionName { get; set; }

        public string YearOrVersionNote { get; set; }

        public bool IsArchived { get; set; }

        public int PracticeCount { get; set; }
    }

    public sealed class AtlasPageSummary
    {
        public Guid Id { get; set; }

        public int PageNumber { get; set; }

        public string PrintedPageLabel { get; set; }

        public double WidthPoints { get; set; }

        public double HeightPoints { get; set; }
    }

    public interface ITaxonomyRepository
    {
        IReadOnlyList<TaxonomyNode> List(TaxonomyType type);

        Guid Create(TaxonomyType type, string name, bool isSystem);

        void Rename(Guid id, string name);

        IReadOnlyList<Tag> ListTags();

        Guid EnsureTag(string name);
    }

    public interface IPdfSource
    {
        string ComputeFingerprint(string filePath);

        Task<IReadOnlyList<PdfPageInfo>> GetPagesAsync(string filePath, CancellationToken cancellationToken);

        Task<IReadOnlyList<PdfTextBlock>> GetTextBlocksAsync(string filePath, int pageNumber, CancellationToken cancellationToken);

        Task<PdfPageImage> RenderPageAsync(string filePath, int pageNumber, RenderParameters parameters, CancellationToken cancellationToken);
    }

    public sealed class PdfPageInfo
    {
        public int PageNumber { get; set; }

        public double WidthPoints { get; set; }

        public double HeightPoints { get; set; }

        public string PrintedPageLabel { get; set; }
    }

    public sealed class PdfTextBlock
    {
        public string Text { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }
    }

    public sealed class RenderParameters
    {
        public int Dpi { get; set; }

        public double? ClipX { get; set; }

        public double? ClipY { get; set; }

        public double? ClipWidth { get; set; }

        public double? ClipHeight { get; set; }
    }

    public sealed class PdfPageImage
    {
        public int PageNumber { get; set; }

        public int WidthPixels { get; set; }

        public int HeightPixels { get; set; }

        public byte[] PngBytes { get; set; }

        public string CacheKey { get; set; }
    }

    public interface IRecognitionProvider
    {
        string ProviderName { get; }

        Task<RecognitionOutcome> RecognizeAsync(RecognitionTask task, byte[] imagePng, string pageText, CancellationToken cancellationToken);
    }

    public interface IPracticeExtractor
    {
        Task<PageExtractionResult> ExtractAsync(ExtractionInput input, CancellationToken cancellationToken);
    }

    public sealed class ExtractionInput
    {
        public int? PageNumber { get; set; }

        public IReadOnlyList<PdfTextBlock> TextBlocks { get; set; }

        public string PageText { get; set; }

        public string AtlasName { get; set; }

        public string AtlasCode { get; set; }

        public string RegionName { get; set; }

        public IReadOnlyList<string> KnownPartNames { get; set; }
    }
}
