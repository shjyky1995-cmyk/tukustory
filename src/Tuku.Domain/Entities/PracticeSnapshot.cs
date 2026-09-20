namespace Tuku.Domain.Entities
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public sealed class LayerSnapshot
    {
        [JsonConstructor]
        public LayerSnapshot(Guid layerId, string originalText, string currentText)
        {
            LayerId = layerId;
            OriginalText = originalText;
            CurrentText = currentText;
        }

        public Guid LayerId { get; private set; }

        public string OriginalText { get; private set; }

        public string CurrentText { get; private set; }
    }

    public sealed class SourceSnapshot
    {
        [JsonConstructor]
        public SourceSnapshot(Guid sourceId, Guid pageId, ValueObjects.PageRegion region)
        {
            SourceId = sourceId;
            PageId = pageId;
            Region = region;
        }

        public Guid SourceId { get; private set; }

        public Guid PageId { get; private set; }

        public ValueObjects.PageRegion Region { get; private set; }
    }

    public sealed class PracticeSnapshot
    {
        [JsonConstructor]
        public PracticeSnapshot(
            string name,
            string code,
            Guid mainPartId,
            string notes,
            string referenceNote,
            IReadOnlyList<LayerSnapshot> layers,
            IReadOnlyList<SourceSnapshot> sources,
            IReadOnlyList<Guid> tagIds)
        {
            Name = name;
            Code = code;
            MainPartId = mainPartId;
            Notes = notes;
            ReferenceNote = referenceNote;
            Layers = layers;
            Sources = sources;
            TagIds = tagIds;
        }

        public string Name { get; private set; }

        public string Code { get; private set; }

        public Guid MainPartId { get; private set; }

        public string Notes { get; private set; }

        public string ReferenceNote { get; private set; }

        public IReadOnlyList<LayerSnapshot> Layers { get; private set; }

        public IReadOnlyList<SourceSnapshot> Sources { get; private set; }

        public IReadOnlyList<Guid> TagIds { get; private set; }
    }
}
