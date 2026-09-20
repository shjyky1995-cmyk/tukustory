namespace Tuku.Domain.Entities
{
    using System;

    public sealed class PracticeRevision
    {
        private PracticeRevision()
        {
        }

        public Guid Id { get; private set; }

        public Guid PracticeId { get; private set; }

        public int RevisionNumber { get; private set; }

        public string SnapshotJson { get; private set; }

        public string Reason { get; private set; }

        public DateTime CreatedUtc { get; private set; }

        public static PracticeRevision Create(
            Guid id,
            Guid practiceId,
            int revisionNumber,
            string snapshotJson,
            string reason,
            DateTime utcNow)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("修订 ID 不能为空", "id");
            }
            if (practiceId == Guid.Empty)
            {
                throw new ArgumentException("修订必须关联做法", "practiceId");
            }
            if (revisionNumber < 1)
            {
                throw new ArgumentOutOfRangeException("revisionNumber", "修订号从 1 开始递增");
            }
            if (string.IsNullOrEmpty(snapshotJson))
            {
                throw new ArgumentException("修订必须保存完整业务快照", "snapshotJson");
            }

            return new PracticeRevision
            {
                Id = id,
                PracticeId = practiceId,
                RevisionNumber = revisionNumber,
                SnapshotJson = snapshotJson,
                Reason = reason,
                CreatedUtc = utcNow
            };
        }
    }
}
