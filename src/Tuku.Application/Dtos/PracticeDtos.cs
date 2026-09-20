namespace Tuku.Application.Dtos
{
    using System;
    using System.Collections.Generic;

    public sealed class PagedResult<T>
    {
        public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
        }

        public IReadOnlyList<T> Items { get; private set; }

        public int TotalCount { get; private set; }

        public int Page { get; private set; }

        public int PageSize { get; private set; }

        public int TotalPages
        {
            get { return PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize); }
        }
    }

    public sealed class PracticeQuery
    {
        public string Keywords { get; set; }

        public Guid? RegionId { get; set; }

        public Guid? PartId { get; set; }

        public Guid? AtlasId { get; set; }

        public IReadOnlyList<Guid> TagIds { get; set; }

        public bool IncludeArchived { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }

    public sealed class PracticeListItem
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public string PartName { get; set; }

        public string AtlasName { get; set; }

        public bool IsVerified { get; set; }

        public bool IsArchived { get; set; }

        public int CurrentRevision { get; set; }

        public int RankScore { get; set; }
    }

    public sealed class LayerDto
    {
        public Guid Id { get; set; }

        public int Order { get; set; }

        public string OriginalText { get; set; }

        public string CurrentText { get; set; }
    }

    public sealed class SourceDto
    {
        public Guid Id { get; set; }

        public Guid PageId { get; set; }

        public int PageNumber { get; set; }

        public string PrintedPageLabel { get; set; }

        public double? X { get; set; }

        public double? Y { get; set; }

        public double? Width { get; set; }

        public double? Height { get; set; }

        public string RegionSource { get; set; }

        public bool RegionReliable { get; set; }
    }

    public sealed class RevisionSummary
    {
        public int RevisionNumber { get; set; }

        public string Reason { get; set; }

        public DateTime CreatedUtc { get; set; }
    }

    public sealed class PracticeDetail
    {
        public Guid Id { get; set; }

        public Guid? AtlasId { get; set; }

        public string AtlasName { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public Guid MainPartId { get; set; }

        public string PartName { get; set; }

        public string Notes { get; set; }

        public string ReferenceNote { get; set; }

        public bool IsVerified { get; set; }

        public bool IsArchived { get; set; }

        public int CurrentRevision { get; set; }

        public IReadOnlyList<LayerDto> Layers { get; set; }

        public IReadOnlyList<SourceDto> Sources { get; set; }

        public IReadOnlyList<string> TagNames { get; set; }

        public IReadOnlyList<RevisionSummary> Revisions { get; set; }
    }

    public sealed class PracticeEditCommand
    {
        public Guid PracticeId { get; set; }

        public int ExpectedRevision { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public Guid MainPartId { get; set; }

        public string Notes { get; set; }

        public string ReferenceNote { get; set; }

        public IReadOnlyList<LayerEdit> Layers { get; set; }

        public IReadOnlyList<SourceEdit> Sources { get; set; }

        public IReadOnlyList<string> TagNames { get; set; }

        public string Reason { get; set; }

        public sealed class LayerEdit
        {
            public string OriginalText { get; set; }

            public string CurrentText { get; set; }
        }

        public sealed class SourceEdit
        {
            public Guid PageId { get; set; }

            public double? X { get; set; }

            public double? Y { get; set; }

            public double? Width { get; set; }

            public double? Height { get; set; }

            public string RegionSource { get; set; }

            public bool RegionReliable { get; set; }
        }
    }

    public enum PracticeSaveStatus
    {
        Saved = 0,
        RevisionConflict = 1,
        ValidationFailed = 2
    }

    public sealed class PracticeSaveResult
    {
        public PracticeSaveStatus Status { get; set; }

        public int NewRevision { get; set; }

        public IReadOnlyList<string> Errors { get; set; }

        public static PracticeSaveResult Ok(int newRevision)
        {
            return new PracticeSaveResult { Status = PracticeSaveStatus.Saved, NewRevision = newRevision };
        }

        public static PracticeSaveResult Conflict(int currentRevision)
        {
            return new PracticeSaveResult
            {
                Status = PracticeSaveStatus.RevisionConflict,
                NewRevision = currentRevision,
                Errors = new[] { string.Format("修订冲突：当前修订号为 {0}", currentRevision) }
            };
        }

        public static PracticeSaveResult Invalid(IReadOnlyList<string> errors)
        {
            return new PracticeSaveResult { Status = PracticeSaveStatus.ValidationFailed, Errors = errors };
        }
    }
}
