namespace Tuku.UnitTests.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tuku.Domain.Common;
    using Tuku.Domain.Entities;
    using Tuku.Domain.ValueObjects;
    using Xunit;

    public class PracticeCreationTests
    {
        private static readonly Guid PartId = Guid.NewGuid();
        private static readonly DateTime Now = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void ManualPractice_AllowsEmptyCode_ButRequiresNameAndInsertableLayer()
        {
            var result = Practice.TryCreate(
                Guid.NewGuid(),
                null,
                "楼地面做法 A",
                null,
                PartId,
                null,
                null,
                new[] { new Practice.LayerInput("20 厚水泥砂浆", "20 厚水泥砂浆") },
                Enumerable.Empty<Practice.SourceInput>(),
                Enumerable.Empty<Guid>(),
                Now,
                out var practice);

            Assert.True(result.IsValid, string.Join("；", result.Errors));
            Assert.NotNull(practice);
            Assert.Null(practice.Code);
            Assert.Equal(1, practice.CurrentRevision);
            Assert.False(practice.IsVerified);
            Assert.False(practice.HasAtlasSource);
            Assert.Single(practice.Layers);
        }

        [Fact]
        public void Practice_WithoutName_Fails()
        {
            var result = Practice.TryCreate(
                Guid.NewGuid(),
                null,
                "  ",
                "D-01",
                PartId,
                null,
                null,
                new[] { new Practice.LayerInput("text", "text") },
                Enumerable.Empty<Practice.SourceInput>(),
                Enumerable.Empty<Guid>(),
                Now,
                out var practice);

            Assert.False(result.IsValid);
            Assert.Null(practice);
            Assert.Contains(result.Errors, e => e.Contains("名称"));
        }

        [Fact]
        public void Practice_WithoutAnyInsertableLayer_Fails()
        {
            var result = Practice.TryCreate(
                Guid.NewGuid(),
                null,
                "空做法",
                "D-02",
                PartId,
                null,
                null,
                new[] { new Practice.LayerInput("原文", "  ") },
                Enumerable.Empty<Practice.SourceInput>(),
                Enumerable.Empty<Guid>(),
                Now,
                out var practice);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("至少"));
        }

        [Fact]
        public void AtlasPractice_WithoutPageSource_Fails()
        {
            var atlasId = Guid.NewGuid();
            var result = Practice.TryCreate(
                Guid.NewGuid(),
                atlasId,
                "图集做法",
                "D-03",
                PartId,
                null,
                null,
                new[] { new Practice.LayerInput("原文", "原文") },
                Enumerable.Empty<Practice.SourceInput>(),
                Enumerable.Empty<Guid>(),
                Now,
                out var practice);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("原 PDF 页来源"));
        }

        [Fact]
        public void AtlasPractice_WithPageSource_Succeeds()
        {
            var pageId = Guid.NewGuid();
            var result = Practice.TryCreate(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "图集做法",
                "D-04",
                PartId,
                null,
                null,
                new[] { new Practice.LayerInput("原文", "原文") },
                new[] { new Practice.SourceInput(pageId, null) },
                Enumerable.Empty<Guid>(),
                Now,
                out var practice);

            Assert.True(result.IsValid, string.Join("；", result.Errors));
            Assert.True(practice.HasAtlasSource);
            Assert.Single(practice.Sources);
            Assert.Equal(pageId, practice.Sources[0].PageId);
        }
    }

    public class PracticeRevisionTests
    {
        private static readonly Guid PartId = Guid.NewGuid();
        private static readonly DateTime Now = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);

        private static Practice CreatePractice()
        {
            var result = Practice.TryCreate(
                Guid.NewGuid(),
                null,
                "楼地面做法 A",
                "D-01",
                PartId,
                null,
                null,
                new[] { new Practice.LayerInput("20 厚水泥砂浆", "20 厚水泥砂浆") },
                Enumerable.Empty<Practice.SourceInput>(),
                Enumerable.Empty<Guid>(),
                Now,
                out var practice);
            Assert.True(result.IsValid, string.Join("；", result.Errors));
            return practice;
        }

        [Fact]
        public void UpdateContent_WithStaleRevision_ReportsConflict()
        {
            var practice = CreatePractice();

            var result = practice.UpdateContent(
                0,
                "新名称",
                "D-01",
                PartId,
                null,
                null,
                new[] { new Practice.LayerInput("x", "x") },
                Enumerable.Empty<Practice.SourceInput>(),
                Enumerable.Empty<Guid>(),
                "测试",
                Now.AddMinutes(1));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("修订冲突"));
            Assert.Equal("楼地面做法 A", practice.Name);
            Assert.Equal(1, practice.CurrentRevision);
        }

        [Fact]
        public void UpdateContent_Success_IncrementsRevisionAndResetsVerified()
        {
            var practice = CreatePractice();
            practice.SetVerified(true);

            var result = practice.UpdateContent(
                1,
                "楼地面做法 A（改）",
                "D-01",
                PartId,
                "备注",
                null,
                new[]
                {
                    new Practice.LayerInput("20 厚水泥砂浆", "20 厚水泥砂浆"),
                    new Practice.LayerInput("素水泥浆一道", "素水泥浆一道")
                },
                Enumerable.Empty<Practice.SourceInput>(),
                Enumerable.Empty<Guid>(),
                "修正层次",
                Now.AddMinutes(1));

            Assert.True(result.IsValid, string.Join("；", result.Errors));
            Assert.Equal(2, practice.CurrentRevision);
            Assert.False(practice.IsVerified);
            Assert.Equal(2, practice.Layers.Count);
            Assert.Equal("楼地面做法 A（改）", practice.Name);
        }

        [Fact]
        public void UpdateContent_ClearingAllLayers_Fails()
        {
            var practice = CreatePractice();

            var result = practice.UpdateContent(
                1,
                "楼地面做法 A",
                "D-01",
                PartId,
                null,
                null,
                Enumerable.Empty<Practice.LayerInput>(),
                Enumerable.Empty<Practice.SourceInput>(),
                Enumerable.Empty<Guid>(),
                "清空层次",
                Now.AddMinutes(1));

            Assert.False(result.IsValid);
            Assert.Single(practice.Layers);
        }

        [Fact]
        public void RestoreFromSnapshot_CreatesNewRevision_KeepsHistoryAccessible()
        {
            var practice = CreatePractice();
            var originalSnapshot = practice.CreateSnapshot();

            practice.UpdateContent(
                1,
                "改过的名称",
                "D-01",
                PartId,
                null,
                null,
                new[] { new Practice.LayerInput("改过的正文", "改过的正文") },
                Enumerable.Empty<Practice.SourceInput>(),
                Enumerable.Empty<Guid>(),
                "修改",
                Now.AddMinutes(1));
            Assert.Equal("改过的名称", practice.Name);

            practice.RestoreFromSnapshot(originalSnapshot, 2, Now.AddMinutes(2));

            Assert.Equal(3, practice.CurrentRevision);
            Assert.Equal("楼地面做法 A", practice.Name);
            Assert.Equal("20 厚水泥砂浆", practice.Layers[0].CurrentText);
            Assert.False(practice.IsVerified);
        }

        [Fact]
        public void Snapshot_RoundTrip_PreservesOrderAndContent()
        {
            var practice = CreatePractice();
            practice.UpdateContent(
                1,
                "楼地面做法 A",
                "D-01",
                PartId,
                "备注一",
                "参见 12J2",
                new[]
                {
                    new Practice.LayerInput("第一层", "第一层"),
                    new Practice.LayerInput("第二层", "第二层")
                },
                Enumerable.Empty<Practice.SourceInput>(),
                new[] { Guid.NewGuid(), Guid.NewGuid() },
                "重排",
                Now.AddMinutes(1));

            var snapshot = practice.CreateSnapshot();

            Assert.Equal(new[] { "第一层", "第二层" }, snapshot.Layers.Select(l => l.CurrentText).ToArray());
            Assert.Equal("参见 12J2", snapshot.ReferenceNote);
            Assert.Equal(2, snapshot.TagIds.Count);

            practice.UpdateContent(
                2,
                "其他",
                "D-01",
                PartId,
                null,
                null,
                new[] { new Practice.LayerInput("只剩一层", "只剩一层") },
                Enumerable.Empty<Practice.SourceInput>(),
                Enumerable.Empty<Guid>(),
                "再改",
                Now.AddMinutes(2));

            practice.RestoreFromSnapshot(snapshot, 3, Now.AddMinutes(3));

            Assert.Equal("楼地面做法 A", practice.Name);
            Assert.Equal(new[] { "第一层", "第二层" }, practice.Layers.Select(l => l.CurrentText).ToArray());
            Assert.Equal(2, practice.TagIds.Count);
        }
    }
}
