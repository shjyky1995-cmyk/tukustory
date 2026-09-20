namespace Tuku.Domain.Search
{
    using System.Collections.Generic;
    using System.Linq;
    using Tuku.Domain.Entities;

    public sealed class PracticeSearchFields
    {
        internal PracticeSearchFields(
            string nameNormalized,
            string codeNormalized,
            string bodyNormalized,
            string notesNormalized,
            string referenceNormalized,
            IReadOnlyList<string> tagNamesNormalized,
            string atlasNameNormalized,
            string atlasCodeNormalized,
            string mainPartNameNormalized)
        {
            NameNormalized = nameNormalized;
            CodeNormalized = codeNormalized;
            BodyNormalized = bodyNormalized;
            NotesNormalized = notesNormalized;
            ReferenceNormalized = referenceNormalized;
            TagNamesNormalized = tagNamesNormalized;
            AtlasNameNormalized = atlasNameNormalized;
            AtlasCodeNormalized = atlasCodeNormalized;
            MainPartNameNormalized = mainPartNameNormalized;
        }

        public string NameNormalized { get; private set; }

        public string CodeNormalized { get; private set; }

        public string BodyNormalized { get; private set; }

        public string NotesNormalized { get; private set; }

        public string ReferenceNormalized { get; private set; }

        public IReadOnlyList<string> TagNamesNormalized { get; private set; }

        public string AtlasNameNormalized { get; private set; }

        public string AtlasCodeNormalized { get; private set; }

        public string MainPartNameNormalized { get; private set; }

        public static PracticeSearchFields FromStoredFields(
            string nameNormalized,
            string codeNormalized,
            string bodyNormalized,
            string notesNormalized,
            string referenceNormalized,
            IReadOnlyList<string> tagNamesNormalized,
            string atlasNameNormalized,
            string atlasCodeNormalized,
            string mainPartNameNormalized)
        {
            return new PracticeSearchFields(
                nameNormalized,
                codeNormalized,
                bodyNormalized,
                notesNormalized,
                referenceNormalized,
                tagNamesNormalized,
                atlasNameNormalized,
                atlasCodeNormalized,
                mainPartNameNormalized);
        }

        public static PracticeSearchFields FromPractice(
            Practice practice,
            string atlasName,
            string atlasCode,
            string mainPartName,
            IReadOnlyDictionary<System.Guid, string> tagNames)
        {
            var body = string.Join(" ", practice.Layers
                .Where(l => !string.IsNullOrWhiteSpace(l.CurrentText))
                .Select(l => l.CurrentText));
            var tags = practice.TagIds
                .Select(id => tagNames != null && tagNames.ContainsKey(id) ? tagNames[id] : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(TextNormalizer.Normalize)
                .ToList();
            return new PracticeSearchFields(
                TextNormalizer.Normalize(practice.Name),
                TextNormalizer.Normalize(practice.Code),
                TextNormalizer.Normalize(body),
                TextNormalizer.Normalize(practice.Notes),
                TextNormalizer.Normalize(practice.ReferenceNote),
                tags,
                TextNormalizer.Normalize(atlasName),
                TextNormalizer.Normalize(atlasCode),
                TextNormalizer.Normalize(mainPartName));
        }
    }
}
