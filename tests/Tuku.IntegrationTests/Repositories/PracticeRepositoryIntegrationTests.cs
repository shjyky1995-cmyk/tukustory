namespace Tuku.IntegrationTests.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Tuku.Application.Abstractions;
    using Tuku.Application.Dtos;
    using Tuku.Domain.Taxonomy;
    using Tuku.Infrastructure.Repositories;
    using Tuku.Infrastructure.Sqlite;
    using Xunit;

    public class PracticeRepositoryIntegrationTests : IDisposable
    {
        private readonly string databasePath;
        private readonly SqliteConnectionFactory connectionFactory;
        private readonly PracticeRepository repository;
        private readonly TaxonomyRepository taxonomyRepository;
        private readonly Guid partFloor = TaxonomySeeder.PartFloor;
        private readonly Guid partInnerWall = TaxonomySeeder.PartInnerWall;
        private readonly Guid regionGuobiao = TaxonomySeeder.RegionGuobiao;

        public PracticeRepositoryIntegrationTests()
        {
            databasePath = Path.Combine(Path.GetTempPath(), "tuku-it-" + Guid.NewGuid().ToString("N") + ".db");
            var initializer = new DatabaseInitializer(databasePath);
            initializer.Initialize();
            connectionFactory = new SqliteConnectionFactory(databasePath);
            repository = new PracticeRepository(connectionFactory);
            taxonomyRepository = new TaxonomyRepository(connectionFactory);
        }

        [Fact]
        public void Initialize_SeedsDefaultTaxonomy_AndIsIdempotent()
        {
            var parts = taxonomyRepository.List(TaxonomyType.Part);
            Assert.Equal(6, parts.Count);
            Assert.Contains(parts, p => p.Name == "楼地面");
            Assert.Contains(parts, p => p.Name == "其他/待分类");

            var appliedFirst = new DatabaseInitializer(databasePath).Initialize();
            Assert.Empty(appliedFirst);

            var regions = taxonomyRepository.List(TaxonomyType.Region);
            Assert.Contains(regions, r => r.Name == "国标");
        }

        [Fact]
        public void CreateThenQueryAndDetail_RoundTrips()
        {
            var id = repository.Create(
                Guid.NewGuid(),
                null,
                "楼地面做法一",
                "D-01",
                partFloor,
                "备注文字",
                "参见 12J2",
                new[] { new PracticeEditCommand.LayerEdit { OriginalText = "20 厚水泥砂浆", CurrentText = "20 厚水泥砂浆" } },
                Enumerable.Empty<PracticeEditCommand.SourceEdit>(),
                new[] { "防水", "卫生间" },
                "手工录入");

            var query = new PracticeQuery { Keywords = "楼地面", Page = 1, PageSize = 10 };
            var result = repository.Query(query);

            Assert.Equal(1, result.TotalCount);
            var item = result.Items[0];
            Assert.Equal("楼地面做法一", item.Name);
            Assert.Equal("D-01", item.Code);
            Assert.Equal("楼地面", item.PartName);
            Assert.False(item.IsVerified);

            var detail = repository.GetDetail(id);
            Assert.NotNull(detail);
            Assert.Equal("参见 12J2", detail.ReferenceNote);
            Assert.Single(detail.Layers);
            Assert.Equal("20 厚水泥砂浆", detail.Layers[0].CurrentText);
            Assert.Equal(2, detail.TagNames.Count);
            Assert.Contains("防水", detail.TagNames);
            Assert.Contains("卫生间", detail.TagNames);
            Assert.Single(detail.Revisions);
            Assert.Equal(1, detail.CurrentRevision);
        }

        [Fact]
        public void Search_NormalizesFullWidthCodeAndCase()
        {
            repository.Create(
                Guid.NewGuid(),
                null,
                "做法甲",
                "L15J-104",
                partFloor,
                null,
                null,
                new[] { new PracticeEditCommand.LayerEdit { OriginalText = "素土夯实", CurrentText = "素土夯实" } },
                Enumerable.Empty<PracticeEditCommand.SourceEdit>(),
                Enumerable.Empty<string>(),
                "录入");

            var result = repository.Query(new PracticeQuery { Keywords = "ｌ１５ｊ－１０４", Page = 1, PageSize = 10 });

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("做法甲", result.Items[0].Name);
        }

        [Fact]
        public void Search_ExactCodeRanksFirst()
        {
            repository.Create(
                Guid.NewGuid(),
                null,
                "普通做法",
                "X-99",
                partFloor,
                null,
                null,
                new[] { new PracticeEditCommand.LayerEdit { OriginalText = "找平层，参见 d-01 图集", CurrentText = "找平层，参见 d-01 图集" } },
                Enumerable.Empty<PracticeEditCommand.SourceEdit>(),
                Enumerable.Empty<string>(),
                "录入");
            repository.Create(
                Guid.NewGuid(),
                null,
                "编号做法",
                "D-01",
                partFloor,
                null,
                null,
                new[] { new PracticeEditCommand.LayerEdit { OriginalText = "结合层", CurrentText = "结合层" } },
                Enumerable.Empty<PracticeEditCommand.SourceEdit>(),
                Enumerable.Empty<string>(),
                "录入");

            var result = repository.Query(new PracticeQuery { Keywords = "d-01", Page = 1, PageSize = 10 });

            Assert.Equal(2, result.TotalCount);
            Assert.Equal("编号做法", result.Items[0].Name);
            Assert.True(result.Items[0].RankScore > result.Items[1].RankScore);
        }

        [Fact]
        public void Search_PartFilterAndMultiTerm()
        {
            repository.Create(
                Guid.NewGuid(),
                null,
                "楼地面做法一",
                "D-01",
                partFloor,
                null,
                null,
                new[] { new PracticeEditCommand.LayerEdit { OriginalText = "20 厚水泥砂浆", CurrentText = "20 厚水泥砂浆" } },
                Enumerable.Empty<PracticeEditCommand.SourceEdit>(),
                new[] { "防水" },
                "录入");
            repository.Create(
                Guid.NewGuid(),
                null,
                "内墙做法一",
                "D-02",
                partInnerWall,
                null,
                null,
                new[] { new PracticeEditCommand.LayerEdit { OriginalText = "20 厚水泥砂浆", CurrentText = "20 厚水泥砂浆" } },
                Enumerable.Empty<PracticeEditCommand.SourceEdit>(),
                new[] { "防水" },
                "录入");

            var floorOnly = repository.Query(new PracticeQuery { PartId = partFloor, Page = 1, PageSize = 10 });
            Assert.Single(floorOnly.Items);
            Assert.Equal("楼地面做法一", floorOnly.Items[0].Name);

            var multiTerm = repository.Query(new PracticeQuery { Keywords = "水泥砂浆 防水", Page = 1, PageSize = 10 });
            Assert.Equal(2, multiTerm.TotalCount);

            var miss = repository.Query(new PracticeQuery { Keywords = "水泥砂浆 不存在的词", Page = 1, PageSize = 10 });
            Assert.Equal(0, miss.TotalCount);
        }

        [Fact]
        public void Save_WithStaleRevision_ReturnsConflict_AndKeepsContent()
        {
            var id = repository.Create(
                Guid.NewGuid(),
                null,
                "楼地面做法一",
                "D-01",
                partFloor,
                null,
                null,
                new[] { new PracticeEditCommand.LayerEdit { OriginalText = "20 厚水泥砂浆", CurrentText = "20 厚水泥砂浆" } },
                Enumerable.Empty<PracticeEditCommand.SourceEdit>(),
                Enumerable.Empty<string>(),
                "录入");

            var conflict = repository.Save(new PracticeEditCommand
            {
                PracticeId = id,
                ExpectedRevision = 5,
                Name = "改坏的名字",
                Code = "D-01",
                MainPartId = partFloor,
                Layers = new[] { new PracticeEditCommand.LayerEdit { OriginalText = "x", CurrentText = "x" } }
            });

            Assert.Equal(PracticeSaveStatus.RevisionConflict, conflict.Status);
            var detail = repository.GetDetail(id);
            Assert.Equal("楼地面做法一", detail.Name);
            Assert.Equal(1, detail.CurrentRevision);
        }

        [Fact]
        public void Save_ThenRestore_CreatesNewRevisions()
        {
            var id = repository.Create(
                Guid.NewGuid(),
                null,
                "楼地面做法一",
                "D-01",
                partFloor,
                null,
                null,
                new[] { new PracticeEditCommand.LayerEdit { OriginalText = "20 厚水泥砂浆", CurrentText = "20 厚水泥砂浆" } },
                Enumerable.Empty<PracticeEditCommand.SourceEdit>(),
                Enumerable.Empty<string>(),
                "录入");

            var saved = repository.Save(new PracticeEditCommand
            {
                PracticeId = id,
                ExpectedRevision = 1,
                Name = "楼地面做法一（改）",
                Code = "D-01",
                MainPartId = partFloor,
                Notes = "改过的备注",
                Layers = new[] { new PracticeEditCommand.LayerEdit { OriginalText = "30 厚水泥砂浆", CurrentText = "30 厚水泥砂浆" } },
                Reason = "修正厚度"
            });
            Assert.Equal(PracticeSaveStatus.Saved, saved.Status);
            Assert.Equal(2, saved.NewRevision);

            var restored = repository.RestoreRevision(id, 1, 2, "恢复误修改");
            Assert.Equal(PracticeSaveStatus.Saved, restored.Status);
            Assert.Equal(3, restored.NewRevision);

            var detail = repository.GetDetail(id);
            Assert.Equal("楼地面做法一", detail.Name);
            Assert.Equal("20 厚水泥砂浆", detail.Layers[0].CurrentText);
            Assert.False(detail.IsVerified);
            Assert.Equal(3, detail.Revisions.Count);
        }

        [Fact]
        public void Edit_ResetsVerifiedStatus()
        {
            var id = repository.Create(
                Guid.NewGuid(),
                null,
                "楼地面做法一",
                "D-01",
                partFloor,
                null,
                null,
                new[] { new PracticeEditCommand.LayerEdit { OriginalText = "20 厚水泥砂浆", CurrentText = "20 厚水泥砂浆" } },
                Enumerable.Empty<PracticeEditCommand.SourceEdit>(),
                Enumerable.Empty<string>(),
                "录入");

            Assert.Equal(PracticeSaveStatus.Saved, repository.SetVerified(id, true).Status);
            Assert.True(repository.GetDetail(id).IsVerified);

            repository.Save(new PracticeEditCommand
            {
                PracticeId = id,
                ExpectedRevision = 1,
                Name = "楼地面做法一",
                Code = "D-01",
                MainPartId = partFloor,
                Layers = new[] { new PracticeEditCommand.LayerEdit { OriginalText = "20 厚水泥砂浆", CurrentText = "25 厚水泥砂浆" } },
                Reason = "改正文"
            });

            Assert.False(repository.GetDetail(id).IsVerified);
        }

        [Fact]
        public void Validation_FailsWithoutInsertableLayer()
        {
            var id = Guid.NewGuid();
            var exception = Assert.Throws<InvalidOperationException>(() => repository.Create(
                id,
                null,
                "空做法",
                null,
                partFloor,
                null,
                null,
                new[] { new PracticeEditCommand.LayerEdit { OriginalText = "有原文", CurrentText = "  " } },
                Enumerable.Empty<PracticeEditCommand.SourceEdit>(),
                Enumerable.Empty<string>(),
                "录入"));

            Assert.Contains("至少", exception.Message);
        }

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            foreach (var suffix in new[] { string.Empty, "-wal", "-shm" })
            {
                var path = databasePath + suffix;
                if (File.Exists(path))
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (IOException)
                    {
                        // 临时测试库残留由系统清理，不影响测试结论
                    }
                }
            }
        }
    }
}
