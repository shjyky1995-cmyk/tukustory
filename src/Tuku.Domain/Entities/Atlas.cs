namespace Tuku.Domain.Entities
{
    using System;
    using Tuku.Domain.Common;

    public sealed class Atlas
    {
        private Atlas()
        {
        }

        public Guid Id { get; private set; }

        public string Name { get; private set; }

        public string Code { get; private set; }

        public Guid RegionTaxonomyId { get; private set; }

        public string YearOrVersionNote { get; private set; }

        public string OriginalFileFingerprint { get; private set; }

        public string ManagedFilePath { get; private set; }

        public bool IsArchived { get; private set; }

        public static ValidationResult TryCreate(
            Guid id,
            string name,
            string code,
            Guid regionTaxonomyId,
            string yearOrVersionNote,
            string originalFileFingerprint,
            string managedFilePath,
            out Atlas atlas)
        {
            atlas = null;
            var errors = new System.Collections.Generic.List<string>();
            if (id == Guid.Empty)
            {
                errors.Add("图集 ID 不能为空");
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("图集名称不能为空");
            }
            if (regionTaxonomyId == Guid.Empty)
            {
                errors.Add("图集必须指定地区分类");
            }
            if (string.IsNullOrWhiteSpace(originalFileFingerprint))
            {
                errors.Add("图集必须记录原文件指纹");
            }
            if (string.IsNullOrWhiteSpace(managedFilePath))
            {
                errors.Add("图集必须记录受管理文件路径");
            }
            if (errors.Count > 0)
            {
                return ValidationResult.Fail(errors);
            }

            atlas = new Atlas
            {
                Id = id,
                Name = name.Trim(),
                Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
                RegionTaxonomyId = regionTaxonomyId,
                YearOrVersionNote = string.IsNullOrWhiteSpace(yearOrVersionNote) ? null : yearOrVersionNote.Trim(),
                OriginalFileFingerprint = originalFileFingerprint,
                ManagedFilePath = managedFilePath
            };
            return ValidationResult.Ok;
        }

        public void Archive()
        {
            IsArchived = true;
        }
    }
}
