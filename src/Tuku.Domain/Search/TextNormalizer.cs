namespace Tuku.Domain.Search
{
    using System.Text;

    public static class TextNormalizer
    {
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(text.Length);
            var pendingSpace = false;
            foreach (var character in text)
            {
                var halfWidth = ToHalfWidth(character);
                if (char.IsWhiteSpace(halfWidth))
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }

                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(char.ToLowerInvariant(halfWidth));
            }

            return builder.ToString();
        }

        public static bool ContainsAllTerms(string haystack, System.Collections.Generic.IReadOnlyList<string> terms)
        {
            if (terms == null || terms.Count == 0)
            {
                return true;
            }

            foreach (var term in terms)
            {
                if (haystack.IndexOf(term, System.StringComparison.Ordinal) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static char ToHalfWidth(char character)
        {
            if (character >= '\uFF01' && character <= '\uFF5E')
            {
                return (char)(character - 0xFEE0);
            }

            if (character == '\u3000')
            {
                return ' ';
            }

            return character;
        }
    }
}
