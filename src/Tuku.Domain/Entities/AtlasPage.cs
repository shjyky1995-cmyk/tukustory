namespace Tuku.Domain.Entities
{
    using System;

    public sealed class AtlasPage
    {
        private AtlasPage()
        {
        }

        public Guid Id { get; private set; }

        public Guid AtlasId { get; private set; }

        public int PageNumber { get; private set; }

        public string PrintedPageLabel { get; private set; }

        public double WidthPoints { get; private set; }

        public double HeightPoints { get; private set; }

        public string ImageCacheKey { get; private set; }

        public static AtlasPage Create(
            Guid id,
            Guid atlasId,
            int pageNumber,
            string printedPageLabel,
            double widthPoints,
            double heightPoints,
            string imageCacheKey)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("页面 ID 不能为空", "id");
            }
            if (atlasId == Guid.Empty)
            {
                throw new ArgumentException("页面必须关联图集", "atlasId");
            }
            if (pageNumber < 1)
            {
                throw new ArgumentOutOfRangeException("pageNumber", "PDF 页序从 1 开始");
            }

            return new AtlasPage
            {
                Id = id,
                AtlasId = atlasId,
                PageNumber = pageNumber,
                PrintedPageLabel = string.IsNullOrWhiteSpace(printedPageLabel) ? null : printedPageLabel.Trim(),
                WidthPoints = widthPoints,
                HeightPoints = heightPoints,
                ImageCacheKey = imageCacheKey
            };
        }
    }
}
