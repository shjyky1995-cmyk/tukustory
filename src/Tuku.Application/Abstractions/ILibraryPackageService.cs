namespace Tuku.Application.Abstractions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class PackageManifest
    {
        public int FormatVersion { get; set; }

        public DateTime ExportedUtc { get; set; }

        public string ExportedBy { get; set; }

        public int AtlasCount { get; set; }

        public int PracticeCount { get; set; }

        public List<PackageFileEntry> Files { get; set; }
    }

    public sealed class PackageFileEntry
    {
        public string RelativePath { get; set; }

        public long SizeBytes { get; set; }

        public string Sha256 { get; set; }
    }

    public sealed class ExportRequest
    {
        public IReadOnlyList<Guid> AtlasIds { get; set; }

        public IReadOnlyList<Guid> PracticeIds { get; set; }

        public bool IncludeHistory { get; set; }

        public bool IncludeOriginalFiles { get; set; }

        public string OutputPath { get; set; }
    }

    public sealed class ExportResult
    {
        public bool Success { get; set; }

        public string PackagePath { get; set; }

        public string Error { get; set; }
    }

    public sealed class ImportPreview
    {
        public int NewAtlases { get; set; }

        public int DuplicateAtlases { get; set; }

        public int ConflictingAtlases { get; set; }

        public int NewPractices { get; set; }

        public int DuplicatePractices { get; set; }

        public int ConflictingPractices { get; set; }

        public List<string> Warnings { get; set; }
    }

    public sealed class ImportSelection
    {
        public bool ImportNewItems { get; set; }

        public bool KeepConflictingCopies { get; set; }
    }

    public sealed class ImportResult
    {
        public bool Success { get; set; }

        public int ImportedPractices { get; set; }

        public int SkippedDuplicates { get; set; }

        public int ConflictedCopies { get; set; }

        public string Error { get; set; }
    }

    public interface ILibraryPackageService
    {
        Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken);

        Task<ImportPreview> PreviewAsync(string packagePath, CancellationToken cancellationToken);

        Task<ImportResult> ImportAsync(string packagePath, ImportSelection selection, CancellationToken cancellationToken);
    }
}
