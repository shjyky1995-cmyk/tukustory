namespace Tuku.Infrastructure.Sqlite
{
    using System;
    using Microsoft.Data.Sqlite;
    using Tuku.Domain.Taxonomy;

    public sealed class TaxonomySeeder
    {
        public static readonly Guid RegionGuobiao = new Guid("11111111-1111-1111-1111-111111111111");
        public static readonly Guid PartFloor = new Guid("22222222-2222-2222-2222-222222222222");
        public static readonly Guid PartInnerWall = new Guid("22222222-2222-2222-2222-222222222223");
        public static readonly Guid PartOuterWall = new Guid("22222222-2222-2222-2222-222222222224");
        public static readonly Guid PartRoof = new Guid("22222222-2222-2222-2222-222222222225");
        public static readonly Guid PartCeiling = new Guid("22222222-2222-2222-2222-222222222226");
        public static readonly Guid PartOther = new Guid("22222222-2222-2222-2222-222222222227");

        private readonly SqliteConnectionFactory connectionFactory;

        public TaxonomySeeder(SqliteConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public void EnsureDefaults()
        {
            using (var connection = connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                EnsureNode(connection, transaction, RegionGuobiao, TaxonomyType.Region, "国标", true);
                EnsureNode(connection, transaction, PartFloor, TaxonomyType.Part, "楼地面", true);
                EnsureNode(connection, transaction, PartInnerWall, TaxonomyType.Part, "内墙面", true);
                EnsureNode(connection, transaction, PartOuterWall, TaxonomyType.Part, "外墙面", true);
                EnsureNode(connection, transaction, PartRoof, TaxonomyType.Part, "屋面", true);
                EnsureNode(connection, transaction, PartCeiling, TaxonomyType.Part, "顶棚", true);
                EnsureNode(connection, transaction, PartOther, TaxonomyType.Part, "其他/待分类", true);
                transaction.Commit();
            }
        }

        private static void EnsureNode(SqliteConnection connection, SqliteTransaction transaction, Guid id, TaxonomyType type, string name, bool isSystem)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "INSERT OR IGNORE INTO taxonomy_nodes (id, type, name, is_system) VALUES ($id, $type, $name, $isSystem)";
                command.Parameters.AddWithValue("$id", id.ToString());
                command.Parameters.AddWithValue("$type", (int)type);
                command.Parameters.AddWithValue("$name", name);
                command.Parameters.AddWithValue("$isSystem", isSystem ? 1 : 0);
                command.ExecuteNonQuery();
            }
        }
    }
}
