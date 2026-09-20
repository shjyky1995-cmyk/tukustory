namespace Tuku.UnitTests.Application
{
    using System;
    using System.Collections.Generic;
    using Tuku.Application.Search;
    using Tuku.Domain.Search;
    using Xunit;

    public class SearchRankerTests
    {
        private static PracticeSearchFields CreateFields(
            string name,
            string code,
            string body,
            string? notes = null,
            string? reference = null,
            string? partName = null,
            string? atlasName = null,
            string? atlasCode = null,
            IEnumerable<string>? tags = null)
        {
            var practice = new PracticeSearchFieldsStub(name, code, body, notes, reference, partName, atlasName, atlasCode, tags);
            return practice.Build();
        }

        [Fact]
        public void ExactCodeMatch_RanksAboveNameMatch()
        {
            var fields = CreateFields("楼地面做法", "d-01", "20 厚水泥砂浆");

            var codeScore = SearchRanker.Score(fields, new[] { "d-01" });
            var nameScore = SearchRanker.Score(fields, new[] { "楼地面" });

            Assert.Equal(SearchRanker.CodeExactMatch, codeScore);
            Assert.Equal(SearchRanker.NameMatch, nameScore);
            Assert.True(codeScore > nameScore);
        }

        [Fact]
        public void BodyMatch_RanksBelowNameMatch()
        {
            var fields = CreateFields("楼地面做法", "d-01", "20 厚水泥砂浆");

            var bodyScore = SearchRanker.Score(fields, new[] { "水泥砂浆" });

            Assert.Equal(SearchRanker.TextMatch, bodyScore);
        }

        [Fact]
        public void MultiTerm_AllTermsMustMatchSomewhere()
        {
            var fields = CreateFields("楼地面做法", "d-01", "20 厚水泥砂浆", notes: "卫生间", tags: new[] { "防水" });

            var bothHit = SearchRanker.Score(fields, new[] { "楼地面", "防水" });
            var oneMisses = SearchRanker.Score(fields, new[] { "楼地面", "不存在的词" });

            Assert.Equal(SearchRanker.NameMatch, bothHit);
            Assert.Equal(SearchRanker.NoMatch, oneMisses);
        }

        [Fact]
        public void EmptyTerms_MeanFilterOnly_NoMatchScore()
        {
            var fields = CreateFields("楼地面做法", "d-01", "20 厚水泥砂浆");

            Assert.Equal(SearchRanker.NoMatch, SearchRanker.Score(fields, new string[0]));
        }

        private sealed class PracticeSearchFieldsStub
        {
            private readonly string name;
            private readonly string code;
            private readonly string body;
            private readonly string? notes;
            private readonly string? reference;
            private readonly string? partName;
            private readonly string? atlasName;
            private readonly string? atlasCode;
            private readonly IEnumerable<string>? tags;

            public PracticeSearchFieldsStub(
                string name,
                string code,
                string body,
                string? notes,
                string? reference,
                string? partName,
                string? atlasName,
                string? atlasCode,
                IEnumerable<string>? tags)
            {
                this.name = name;
                this.code = code;
                this.body = body;
                this.notes = notes;
                this.reference = reference;
                this.partName = partName;
                this.atlasName = atlasName;
                this.atlasCode = atlasCode;
                this.tags = tags;
            }

            public PracticeSearchFields Build()
            {
                return new PracticeSearchFields(
                    TextNormalizer.Normalize(name),
                    TextNormalizer.Normalize(code),
                    TextNormalizer.Normalize(body),
                    TextNormalizer.Normalize(notes),
                    TextNormalizer.Normalize(reference),
                    tags == null ? new List<string>() : new List<string>(System.Linq.Enumerable.Select(tags, TextNormalizer.Normalize)),
                    TextNormalizer.Normalize(atlasName),
                    TextNormalizer.Normalize(atlasCode),
                    TextNormalizer.Normalize(partName));
            }
        }
    }
}
