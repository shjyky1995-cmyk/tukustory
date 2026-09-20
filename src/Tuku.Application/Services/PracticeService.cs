namespace Tuku.Application.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tuku.Application.Abstractions;
    using Tuku.Application.Dtos;
    using Tuku.Domain.Common;
    using Tuku.Domain.Entities;
    using Tuku.Domain.ValueObjects;

    public sealed class PracticeService
    {
        private readonly IPracticeRepository practiceRepository;

        public PracticeService(IPracticeRepository practiceRepository)
        {
            this.practiceRepository = practiceRepository;
        }

        public PagedResult<PracticeListItem> Search(PracticeQuery query)
        {
            return practiceRepository.Query(query);
        }

        public PracticeDetail GetDetail(Guid practiceId)
        {
            return practiceRepository.GetDetail(practiceId);
        }

        public IReadOnlyList<RevisionSummary> GetRevisions(Guid practiceId)
        {
            return practiceRepository.GetRevisions(practiceId);
        }

        public RevisionSnapshotDto GetRevisionSnapshot(Guid practiceId, int revisionNumber)
        {
            return practiceRepository.GetRevisionSnapshot(practiceId, revisionNumber);
        }

        public PracticeSaveResult CreateManual(CreatePracticeCommand command)
        {
            if (command == null)
            {
                return PracticeSaveResult.Invalid(new[] { "命令不能为空" });
            }

            var validation = ValidateCreate(command);
            if (!validation.IsValid)
            {
                return PracticeSaveResult.Invalid(validation.Errors);
            }

            practiceRepository.CreatePractice(command);
            return PracticeSaveResult.Ok(1);
        }

        public PracticeSaveResult Edit(PracticeEditCommand command)
        {
            if (command == null)
            {
                return PracticeSaveResult.Invalid(new[] { "命令不能为空" });
            }

            return practiceRepository.Save(command);
        }

        public PracticeSaveResult RestoreRevision(Guid practiceId, int revisionNumber, int expectedRevision)
        {
            return practiceRepository.RestoreRevision(
                practiceId,
                revisionNumber,
                expectedRevision,
                "恢复修订 " + revisionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public PracticeSaveResult SetVerified(Guid practiceId, bool verified)
        {
            return practiceRepository.SetVerified(practiceId, verified);
        }

        private static ValidationResult ValidateCreate(CreatePracticeCommand command)
        {
            Practice ignored;
            return Practice.TryCreate(
                Guid.NewGuid(),
                command.AtlasId,
                command.Name,
                command.Code,
                command.MainPartId,
                command.Notes,
                command.ReferenceNote,
                MapLayers(command.Layers),
                MapSources(command.Sources),
                Enumerable.Empty<Guid>(),
                DateTime.UtcNow,
                out ignored);
        }

        private static IEnumerable<Practice.LayerInput> MapLayers(IEnumerable<PracticeEditCommand.LayerEdit> layers)
        {
            return (layers ?? Enumerable.Empty<PracticeEditCommand.LayerEdit>())
                .Select(l => new Practice.LayerInput(l.OriginalText, l.CurrentText));
        }

        private static IEnumerable<Practice.SourceInput> MapSources(IEnumerable<PracticeEditCommand.SourceEdit> sources)
        {
            return (sources ?? Enumerable.Empty<PracticeEditCommand.SourceEdit>())
                .Select(s => new Practice.SourceInput(
                    s.PageId,
                    s.X.HasValue && s.Y.HasValue && s.Width.HasValue && s.Height.HasValue
                        ? new PageRegion(s.X.Value, s.Y.Value, s.Width.Value, s.Height.Value, ParseRegionSource(s.RegionSource), s.RegionReliable)
                        : null));
        }

        private static RegionSource ParseRegionSource(string value)
        {
            RegionSource parsed;
            if (Enum.TryParse(value, true, out parsed))
            {
                return parsed;
            }

            return RegionSource.Unknown;
        }
    }
}
