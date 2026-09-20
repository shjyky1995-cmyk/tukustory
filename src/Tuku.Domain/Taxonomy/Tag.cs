namespace Tuku.Domain.Taxonomy
{
    using System;

    public sealed class Tag
    {
        private Tag()
        {
        }

        public Guid Id { get; private set; }

        public string Name { get; private set; }

        public static Tag Create(Guid id, string name)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("标签 ID 不能为空", "id");
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("标签名称不能为空", "name");
            }

            return new Tag
            {
                Id = id,
                Name = name.Trim()
            };
        }
    }
}
