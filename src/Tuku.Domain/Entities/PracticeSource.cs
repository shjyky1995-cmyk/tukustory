namespace Tuku.Domain.Entities
{
    using System;
    using System.Collections.Generic;
    using Tuku.Domain.Common;
    using Tuku.Domain.ValueObjects;

    public sealed class PracticeSource
    {
        private PracticeSource()
        {
        }

        public Guid Id { get; private set; }

        public Guid PracticeId { get; private set; }

        public Guid PageId { get; private set; }

        public PageRegion Region { get; private set; }

        public static PracticeSource Create(Guid id, Guid practiceId, Guid pageId, PageRegion region)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("来源 ID 不能为空", "id");
            }
            if (practiceId == Guid.Empty)
            {
                throw new ArgumentException("来源必须关联做法", "practiceId");
            }
            if (pageId == Guid.Empty)
            {
                throw new ArgumentException("来源必须关联页面", "pageId");
            }

            var source = new PracticeSource
            {
                Id = id,
                PracticeId = practiceId,
                PageId = pageId,
                Region = region
            };
            if (region != null)
            {
                var check = PageRegion.Validate(region);
                if (!check.IsValid)
                {
                    throw new ArgumentException(string.Join("；", check.Errors), "region");
                }
            }

            return source;
        }
    }
}
