namespace Tuku.Domain.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tuku.Domain.Common;

    public sealed class Practice
    {
        private readonly List<PracticeLayer> layers = new List<PracticeLayer>();
        private readonly List<PracticeSource> sources = new List<PracticeSource>();
        private readonly List<Guid> tagIds = new List<Guid>();

        private Practice()
        {
        }

        public Guid Id { get; private set; }

        public Guid? AtlasId { get; private set; }

        public string Name { get; private set; }

        public string Code { get; private set; }

        public Guid MainPartId { get; private set; }

        public string Notes { get; private set; }

        public string ReferenceNote { get; private set; }

        public bool IsVerified { get; private set; }

        public bool IsArchived { get; private set; }

        public int CurrentRevision { get; private set; }

        public DateTime CreatedUtc { get; private set; }

        public DateTime UpdatedUtc { get; private set; }

        public IReadOnlyList<PracticeLayer> Layers
        {
            get { return layers; }
        }

        public IReadOnlyList<PracticeSource> Sources
        {
            get { return sources; }
        }

        public IReadOnlyList<Guid> TagIds
        {
            get { return tagIds; }
        }

        public bool HasAtlasSource
        {
            get { return AtlasId.HasValue; }
        }

        public static ValidationResult TryCreate(
            Guid id,
            Guid? atlasId,
            string name,
            string code,
            Guid mainPartId,
            string notes,
            string referenceNote,
            IEnumerable<LayerInput> layerInputs,
            IEnumerable<SourceInput> sourceInputs,
            IEnumerable<Guid> tagIds,
            DateTime utcNow,
            out Practice practice)
        {
            practice = null;
            var errors = new List<string>();
            if (id == Guid.Empty)
            {
                errors.Add("做法 ID 不能为空");
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("做法名称不能为空");
            }
            if (mainPartId == Guid.Empty)
            {
                errors.Add("做法必须指定主部位");
            }

            var layers = (layerInputs ?? Enumerable.Empty<LayerInput>()).ToList();
            if (!layers.Any(l => !string.IsNullOrWhiteSpace(l.CurrentText)))
            {
                errors.Add("做法至少需要一层可插入正文（当前文字非空）");
            }

            var sourceList = (sourceInputs ?? Enumerable.Empty<SourceInput>()).ToList();
            if (atlasId.HasValue && sourceList.Count == 0)
            {
                errors.Add("从图集创建的做法必须关联至少一个原 PDF 页来源");
            }
            foreach (var source in sourceList)
            {
                if (source.PageId == Guid.Empty)
                {
                    errors.Add("来源必须关联页面");
                }
                if (source.Region != null)
                {
                    var regionCheck = ValueObjects.PageRegion.Validate(source.Region);
                    if (!regionCheck.IsValid)
                    {
                        errors.AddRange(regionCheck.Errors);
                    }
                }
            }

            if (errors.Count > 0)
            {
                return ValidationResult.Fail(errors);
            }

            var entity = new Practice
            {
                Id = id,
                AtlasId = atlasId,
                Name = name.Trim(),
                Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
                MainPartId = mainPartId,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                ReferenceNote = string.IsNullOrWhiteSpace(referenceNote) ? null : referenceNote.Trim(),
                IsVerified = false,
                IsArchived = false,
                CurrentRevision = 1,
                CreatedUtc = utcNow,
                UpdatedUtc = utcNow
            };

            var order = 1;
            foreach (var input in layers)
            {
                entity.layers.Add(PracticeLayer.Create(Guid.NewGuid(), id, order, input.OriginalText, input.CurrentText));
                order++;
            }

            foreach (var input in sourceList)
            {
                entity.sources.Add(PracticeSource.Create(Guid.NewGuid(), id, input.PageId, input.Region));
            }

            foreach (var tagId in tagIds ?? Enumerable.Empty<Guid>())
            {
                if (tagId != Guid.Empty && !entity.tagIds.Contains(tagId))
                {
                    entity.tagIds.Add(tagId);
                }
            }

            practice = entity;
            return ValidationResult.Ok;
        }

        public ValidationResult UpdateContent(
            int expectedRevision,
            string name,
            string code,
            Guid mainPartId,
            string notes,
            string referenceNote,
            IEnumerable<LayerInput> layerInputs,
            IEnumerable<SourceInput> sourceInputs,
            IEnumerable<Guid> newTagIds,
            string reason,
            DateTime utcNow)
        {
            if (expectedRevision != CurrentRevision)
            {
                return ValidationResult.Fail(
                    string.Format("修订冲突：预期修订号 {0}，当前修订号 {1}", expectedRevision, CurrentRevision));
            }

            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("做法名称不能为空");
            }
            if (mainPartId == Guid.Empty)
            {
                errors.Add("做法必须指定主部位");
            }

            var newLayers = (layerInputs ?? Enumerable.Empty<LayerInput>()).ToList();
            if (!newLayers.Any(l => !string.IsNullOrWhiteSpace(l.CurrentText)))
            {
                errors.Add("做法至少需要一层可插入正文（当前文字非空）");
            }

            var newSources = (sourceInputs ?? Enumerable.Empty<SourceInput>()).ToList();
            if (AtlasId.HasValue && newSources.Count == 0)
            {
                errors.Add("从图集创建的做法必须关联至少一个原 PDF 页来源");
            }
            foreach (var source in newSources)
            {
                if (source.PageId == Guid.Empty)
                {
                    errors.Add("来源必须关联页面");
                }
                if (source.Region != null)
                {
                    var regionCheck = ValueObjects.PageRegion.Validate(source.Region);
                    if (!regionCheck.IsValid)
                    {
                        errors.AddRange(regionCheck.Errors);
                    }
                }
            }

            if (errors.Count > 0)
            {
                return ValidationResult.Fail(errors);
            }

            Name = name.Trim();
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
            MainPartId = mainPartId;
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            ReferenceNote = string.IsNullOrWhiteSpace(referenceNote) ? null : referenceNote.Trim();

            layers.Clear();
            var order = 1;
            foreach (var input in newLayers)
            {
                layers.Add(PracticeLayer.Create(Guid.NewGuid(), Id, order, input.OriginalText, input.CurrentText));
                order++;
            }

            sources.Clear();
            foreach (var input in newSources)
            {
                sources.Add(PracticeSource.Create(Guid.NewGuid(), Id, input.PageId, input.Region));
            }

            tagIds.Clear();
            foreach (var tagId in newTagIds ?? Enumerable.Empty<Guid>())
            {
                if (tagId != Guid.Empty && !tagIds.Contains(tagId))
                {
                    tagIds.Add(tagId);
                }
            }

            CurrentRevision++;
            IsVerified = false;
            UpdatedUtc = utcNow;
            return ValidationResult.Ok;
        }

        public void RestoreFromSnapshot(PracticeSnapshot snapshot, int expectedRevision, DateTime utcNow)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException("snapshot");
            }
            if (expectedRevision != CurrentRevision)
            {
                throw new InvalidOperationException(
                    string.Format("修订冲突：预期修订号 {0}，当前修订号 {1}", expectedRevision, CurrentRevision));
            }

            ApplySnapshot(snapshot);
            CurrentRevision++;
            IsVerified = false;
            UpdatedUtc = utcNow;
        }

        public void ApplyCandidateSnapshot(PracticeSnapshot snapshot, int expectedRevision, DateTime utcNow)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException("snapshot");
            }
            if (expectedRevision != CurrentRevision)
            {
                throw new InvalidOperationException(
                    string.Format("修订冲突：预期修订号 {0}，当前修订号 {1}", expectedRevision, CurrentRevision));
            }

            ApplySnapshot(snapshot);
            CurrentRevision++;
            IsVerified = false;
            UpdatedUtc = utcNow;
        }

        public void SetVerified(bool verified)
        {
            IsVerified = verified;
        }

        public void Archive()
        {
            IsArchived = true;
        }

        public PracticeSnapshot CreateSnapshot()
        {
            var layerSnapshots = layers
                .OrderBy(l => l.Order)
                .Select(l => new LayerSnapshot(l.Id, l.OriginalText, l.CurrentText))
                .ToList();
            var sourceSnapshots = sources
                .Select(s => new SourceSnapshot(s.Id, s.PageId, s.Region))
                .ToList();
            return new PracticeSnapshot(Name, Code, MainPartId, Notes, ReferenceNote, layerSnapshots, sourceSnapshots, tagIds.ToList());
        }

        private void ApplySnapshot(PracticeSnapshot snapshot)
        {
            Name = snapshot.Name;
            Code = snapshot.Code;
            MainPartId = snapshot.MainPartId;
            Notes = snapshot.Notes;
            ReferenceNote = snapshot.ReferenceNote;

            layers.Clear();
            var order = 1;
            foreach (var layer in snapshot.Layers)
            {
                layers.Add(PracticeLayer.Create(layer.LayerId == Guid.Empty ? Guid.NewGuid() : layer.LayerId, Id, order, layer.OriginalText, layer.CurrentText));
                order++;
            }

            sources.Clear();
            foreach (var source in snapshot.Sources)
            {
                sources.Add(PracticeSource.Create(source.SourceId == Guid.Empty ? Guid.NewGuid() : source.SourceId, Id, source.PageId, source.Region));
            }

            tagIds.Clear();
            foreach (var tagId in snapshot.TagIds)
            {
                if (tagId != Guid.Empty && !tagIds.Contains(tagId))
                {
                    tagIds.Add(tagId);
                }
            }
        }

        public sealed class LayerInput
        {
            public LayerInput(string originalText, string currentText)
            {
                OriginalText = originalText;
                CurrentText = currentText;
            }

            public string OriginalText { get; private set; }

            public string CurrentText { get; private set; }
        }

        public sealed class SourceInput
        {
            public SourceInput(Guid pageId, ValueObjects.PageRegion region)
            {
                PageId = pageId;
                Region = region;
            }

            public Guid PageId { get; private set; }

            public ValueObjects.PageRegion Region { get; private set; }
        }
    }
}
