namespace Tuku.UnitTests.Domain
{
    using System;
    using System.Collections.Generic;
    using Tuku.Domain.Entities;
    using Tuku.Domain.Search;
    using Xunit;

    public class PracticeSearchFieldsTests
    {
        [Fact]
        public void ComposesNormalizedFieldsFromPracticeAtlasAndTags()
        {
            var partId = Guid.NewGuid();
            var tagId = Guid.NewGuid();
            var tagNames = new Dictionary<Guid, string> { { tagId, "防水" } };
            var result = Practice.TryCreate(
                Guid.NewGuid(),
                null,
                "楼地面做法 Ａ",
                "Ｄ－０１",
                partId,
                "备注 ＴＥＳＴ",
                null,
                new[] { new Practice.LayerInput("原文", "20 厚水泥砂浆") },
                Enumerable.Empty<Practice.SourceInput>(),
                new[] { tagId },
                new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc),
                out var practice);
            Assert.True(result.IsValid, string.Join("；", result.Errors));

            var fields = PracticeSearchFields.FromPractice(practice, "山东 图集", "Ｌ１５Ｊ", "楼地面", tagNames);

            Assert.Equal("楼地面做法 a", fields.NameNormalized);
            Assert.Equal("d-01", fields.CodeNormalized);
            Assert.Equal("20 厚水泥砂浆", fields.BodyNormalized);
            Assert.Equal("备注 test", fields.NotesNormalized);
            Assert.Equal(new[] { "防水" }, fields.TagNamesNormalized);
            Assert.Equal("山东 图集", fields.AtlasNameNormalized);
            Assert.Equal("l15j", fields.AtlasCodeNormalized);
            Assert.Equal("楼地面", fields.MainPartNameNormalized);
        }
    }
}
