namespace Tuku.Application.Search
{
    using System.Collections.Generic;
    using Tuku.Domain.Search;

    public static class SearchRanker
    {
        public const int NoMatch = 0;
        public const int TextMatch = 1;
        public const int NameMatch = 2;
        public const int CodeExactMatch = 3;

        public static int Score(PracticeSearchFields fields, IReadOnlyList<string> normalizedTerms)
        {
            if (fields == null || normalizedTerms == null || normalizedTerms.Count == 0)
            {
                return NoMatch;
            }

            var best = 0;
            foreach (var term in normalizedTerms)
            {
                var termScore = ScoreTerm(fields, term);
                if (termScore == NoMatch)
                {
                    return NoMatch;
                }

                if (termScore > best)
                {
                    best = termScore;
                }
            }

            return best;
        }

        private static int ScoreTerm(PracticeSearchFields fields, string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return TextMatch;
            }

            if (!string.IsNullOrEmpty(fields.CodeNormalized) && fields.CodeNormalized == term)
            {
                return CodeExactMatch;
            }

            if (Contains(fields.NameNormalized, term))
            {
                return NameMatch;
            }

            if (Contains(fields.BodyNormalized, term)
                || Contains(fields.NotesNormalized, term)
                || Contains(fields.ReferenceNormalized, term)
                || Contains(fields.MainPartNameNormalized, term)
                || Contains(fields.AtlasNameNormalized, term)
                || Contains(fields.AtlasCodeNormalized, term))
            {
                return TextMatch;
            }

            if (fields.TagNamesNormalized != null)
            {
                foreach (var tag in fields.TagNamesNormalized)
                {
                    if (Contains(tag, term))
                    {
                        return TextMatch;
                    }
                }
            }

            return NoMatch;
        }

        private static bool Contains(string haystack, string term)
        {
            return !string.IsNullOrEmpty(haystack) && haystack.IndexOf(term, System.StringComparison.Ordinal) >= 0;
        }
    }
}
