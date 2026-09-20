namespace Tuku.Application.Services
{
    using System;
    using System.Collections.Generic;
    using Tuku.Application.Abstractions;
    using Tuku.Domain.Common;
    using Tuku.Domain.Entities;
    using Tuku.Domain.Taxonomy;

    public sealed class AtlasService
    {
        private readonly IAtlasRepository atlasRepository;

        public AtlasService(IAtlasRepository atlasRepository)
        {
            this.atlasRepository = atlasRepository;
        }

        public IReadOnlyList<AtlasSummary> List(bool includeArchived)
        {
            return atlasRepository.ListAtlases(includeArchived);
        }

        public AtlasSummary Get(Guid atlasId)
        {
            return atlasRepository.GetAtlas(atlasId);
        }

        public IReadOnlyList<AtlasPageSummary> GetPages(Guid atlasId)
        {
            return atlasRepository.GetPages(atlasId);
        }

        public ValidationResult Create(
            string name,
            string code,
            Guid regionId,
            string yearOrVersionNote,
            string originalFileFingerprint,
            string managedFilePath,
            out Guid atlasId)
        {
            atlasId = Guid.Empty;
            var result = Atlas.TryCreate(
                Guid.NewGuid(),
                name,
                code,
                regionId,
                yearOrVersionNote,
                originalFileFingerprint,
                managedFilePath,
                out var atlas);
            if (!result.IsValid)
            {
                return result;
            }

            atlasId = atlasRepository.CreateAtlas(
                atlas.Name,
                atlas.Code,
                regionId,
                atlas.YearOrVersionNote,
                originalFileFingerprint,
                managedFilePath);
            return ValidationResult.Ok;
        }

        public void RegisterPage(
            Guid atlasId,
            int pageNumber,
            string printedPageLabel,
            double widthPoints,
            double heightPoints,
            string imageCacheKey)
        {
            atlasRepository.RegisterPage(atlasId, pageNumber, printedPageLabel, widthPoints, heightPoints, imageCacheKey);
        }

        public void Archive(Guid atlasId)
        {
            atlasRepository.ArchiveAtlas(atlasId);
        }
    }

    public sealed class TaxonomyService
    {
        private readonly ITaxonomyRepository taxonomyRepository;

        public TaxonomyService(ITaxonomyRepository taxonomyRepository)
        {
            this.taxonomyRepository = taxonomyRepository;
        }

        public IReadOnlyList<TaxonomyNode> ListRegions()
        {
            return taxonomyRepository.List(TaxonomyType.Region);
        }

        public IReadOnlyList<TaxonomyNode> ListParts()
        {
            return taxonomyRepository.List(TaxonomyType.Part);
        }

        public IReadOnlyList<Tag> ListTags()
        {
            return taxonomyRepository.ListTags();
        }

        public Guid CreateNode(TaxonomyType type, string name)
        {
            return taxonomyRepository.Create(type, name, false);
        }

        public void RenameNode(Guid id, string name)
        {
            taxonomyRepository.Rename(id, name);
        }

        public Guid EnsureTag(string name)
        {
            return taxonomyRepository.EnsureTag(name);
        }
    }
}
