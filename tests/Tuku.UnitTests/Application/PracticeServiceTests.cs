namespace Tuku.UnitTests.Application
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tuku.Application.Abstractions;
    using Tuku.Application.Dtos;
    using Tuku.Application.Services;
    using Xunit;

    internal sealed class FakePracticeRepository : IPracticeRepository
    {
        public List<CreatePracticeCommand> CreatedCommands = new List<CreatePracticeCommand>();
        public List<PracticeEditCommand> SavedCommands = new List<PracticeEditCommand>();
        public List<Guid> RestoredIds = new List<Guid>();
        public List<bool> VerifiedCalls = new List<bool>();
        public PracticeSaveResult NextSaveResult = PracticeSaveResult.Ok(2);
        public Guid NextCreatedId = Guid.NewGuid();

        public Guid CreatePractice(CreatePracticeCommand command)
        {
            CreatedCommands.Add(command);
            return NextCreatedId;
        }

        public PagedResult<PracticeListItem> Query(PracticeQuery query)
        {
            return new PagedResult<PracticeListItem>(new List<PracticeListItem>(), 0, query.Page, query.PageSize);
        }

        public PracticeDetail GetDetail(Guid practiceId)
        {
            return null!;
        }

        public RevisionSnapshotDto GetRevisionSnapshot(Guid practiceId, int revisionNumber)
        {
            return null!;
        }

        public IReadOnlyList<RevisionSummary> GetRevisions(Guid practiceId)
        {
            return new List<RevisionSummary>();
        }

        public PracticeSaveResult Save(PracticeEditCommand command)
        {
            SavedCommands.Add(command);
            return NextSaveResult;
        }

        public PracticeSaveResult RestoreRevision(Guid practiceId, int revisionNumber, int expectedRevision, string reason)
        {
            RestoredIds.Add(practiceId);
            return PracticeSaveResult.Ok(expectedRevision + 1);
        }

        public PracticeSaveResult SetVerified(Guid practiceId, bool verified)
        {
            VerifiedCalls.Add(verified);
            return PracticeSaveResult.Ok(0);
        }
    }

    public class PracticeServiceTests
    {
        private static readonly Guid PartId = Guid.NewGuid();

        private static CreatePracticeCommand ValidCreateCommand()
        {
            return new CreatePracticeCommand
            {
                AtlasId = null,
                Name = "楼地面做法一",
                Code = null,
                MainPartId = PartId,
                Layers = new[] { new PracticeEditCommand.LayerEdit { OriginalText = "20 厚水泥砂浆", CurrentText = "20 厚水泥砂浆" } },
                Sources = Enumerable.Empty<PracticeEditCommand.SourceEdit>().ToList(),
                TagNames = Enumerable.Empty<string>().ToList()
            };
        }

        [Fact]
        public void CreateManual_WithoutInsertableLayer_ReturnsValidationFailed_AndDoesNotPersist()
        {
            var repository = new FakePracticeRepository();
            var service = new PracticeService(repository);
            var command = ValidCreateCommand();
            command.Layers = new[] { new PracticeEditCommand.LayerEdit { OriginalText = "有原文", CurrentText = "   " } };

            var result = service.CreateManual(command);

            Assert.Equal(PracticeSaveStatus.ValidationFailed, result.Status);
            Assert.Empty(repository.CreatedCommands);
        }

        [Fact]
        public void CreateManual_Valid_PersistsOnce()
        {
            var repository = new FakePracticeRepository();
            var service = new PracticeService(repository);

            var result = service.CreateManual(ValidCreateCommand());

            Assert.Equal(PracticeSaveStatus.Saved, result.Status);
            Assert.Single(repository.CreatedCommands);
            Assert.Equal("楼地面做法一", repository.CreatedCommands[0].Name);
        }

        [Fact]
        public void Edit_PassesThroughRepositoryResult()
        {
            var repository = new FakePracticeRepository
            {
                NextSaveResult = PracticeSaveResult.Conflict(4)
            };
            var service = new PracticeService(repository);

            var result = service.Edit(new PracticeEditCommand
            {
                PracticeId = Guid.NewGuid(),
                ExpectedRevision = 1,
                Name = "改名",
                MainPartId = PartId,
                Layers = new[] { new PracticeEditCommand.LayerEdit { OriginalText = "a", CurrentText = "a" } }
            });

            Assert.Equal(PracticeSaveStatus.RevisionConflict, result.Status);
            Assert.Single(repository.SavedCommands);
        }

        [Fact]
        public void RestoreRevision_DelegatesWithReason()
        {
            var repository = new FakePracticeRepository();
            var service = new PracticeService(repository);
            var id = Guid.NewGuid();

            var result = service.RestoreRevision(id, 1, 3);

            Assert.Equal(PracticeSaveStatus.Saved, result.Status);
            Assert.Equal(4, result.NewRevision);
            Assert.Contains(id, repository.RestoredIds);
        }

        [Fact]
        public void SetVerified_Delegates()
        {
            var repository = new FakePracticeRepository();
            var service = new PracticeService(repository);

            service.SetVerified(Guid.NewGuid(), true);

            Assert.Equal(new[] { true }, repository.VerifiedCalls);
        }
    }
}
