namespace Tuku.Domain.Entities
{
    using System;

    public sealed class PracticeLayer
    {
        private PracticeLayer()
        {
        }

        public Guid Id { get; private set; }

        public Guid PracticeId { get; private set; }

        public int Order { get; private set; }

        public string OriginalText { get; private set; }

        public string CurrentText { get; private set; }

        public static PracticeLayer Create(Guid id, Guid practiceId, int order, string originalText, string currentText)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("层次 ID 不能为空", "id");
            }
            if (practiceId == Guid.Empty)
            {
                throw new ArgumentException("层次必须关联做法", "practiceId");
            }
            if (order < 1)
            {
                throw new ArgumentOutOfRangeException("order", "层次排序号从 1 开始连续递增");
            }

            return new PracticeLayer
            {
                Id = id,
                PracticeId = practiceId,
                Order = order,
                OriginalText = originalText,
                CurrentText = currentText
            };
        }
    }
}
