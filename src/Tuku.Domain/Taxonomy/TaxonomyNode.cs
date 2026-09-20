namespace Tuku.Domain.Taxonomy
{
    using System;

    public enum TaxonomyType
    {
        Region = 0,
        Part = 1
    }

    public sealed class TaxonomyNode
    {
        private TaxonomyNode()
        {
        }

        public Guid Id { get; private set; }

        public TaxonomyType Type { get; private set; }

        public string Name { get; private set; }

        public bool IsSystem { get; private set; }

        public static TaxonomyNode Create(Guid id, TaxonomyType type, string name, bool isSystem)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("分类 ID 不能为空", "id");
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("分类名称不能为空", "name");
            }

            return new TaxonomyNode
            {
                Id = id,
                Type = type,
                Name = name.Trim(),
                IsSystem = isSystem
            };
        }

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("分类名称不能为空", "name");
            }

            Name = name.Trim();
        }
    }
}
