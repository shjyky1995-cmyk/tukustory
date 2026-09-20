namespace Tuku.UnitTests.Domain
{
    using Tuku.Domain.Search;
    using Xunit;

    public class TextNormalizerTests
    {
        [Fact]
        public void FullWidth_IsConvertedToHalfWidth()
        {
            Assert.Equal("d-01", TextNormalizer.Normalize("Ｄ－０１"));
        }

        [Fact]
        public void Case_IsFolded()
        {
            Assert.Equal("abc", TextNormalizer.Normalize("AbC"));
        }

        [Fact]
        public void ConsecutiveWhitespace_AndIdeographicSpace_CollapseToSingleSpace()
        {
            Assert.Equal("a b", TextNormalizer.Normalize("a \t\n b"));
            Assert.Equal("a b", TextNormalizer.Normalize("a\u3000b"));
        }

        [Fact]
        public void LeadingAndTrailingWhitespace_IsTrimmed()
        {
            Assert.Equal("abc", TextNormalizer.Normalize("  abc  "));
        }

        [Fact]
        public void EmptyOrNull_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, TextNormalizer.Normalize(null));
            Assert.Equal(string.Empty, TextNormalizer.Normalize("   "));
        }

        [Fact]
        public void ChineseText_PassesThrough()
        {
            Assert.Equal("楼地面做法", TextNormalizer.Normalize("楼地面做法"));
        }
    }
}
