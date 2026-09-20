namespace Tuku.Infrastructure.Repositories
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Data.Sqlite;
    using Tuku.Application.Abstractions;
    using Tuku.Domain.Taxonomy;
    using Tuku.Infrastructure.Sqlite;

    public sealed class TaxonomyRepository : ITaxonomyRepository
    {
        private readonly SqliteConnectionFactory connectionFactory;

        public TaxonomyRepository(SqliteConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public IReadOnlyList<TaxonomyNode> List(TaxonomyType type)
        {
            var nodes = new List<TaxonomyNode>();
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT id, type, name, is_system FROM taxonomy_nodes WHERE type = $type ORDER BY is_system DESC, name";
                    command.Parameters.AddWithValue("$type", (int)type);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nodes.Add(TaxonomyNode.Create(
                                Guid.Parse(reader.GetString(0)),
                                (TaxonomyType)reader.GetInt32(1),
                                reader.GetString(2),
                                reader.GetInt32(3) != 0));
                        }
                    }
                }
            }

            return nodes;
        }

        public Guid Create(TaxonomyType type, string name, bool isSystem)
        {
            var id = Guid.NewGuid();
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "INSERT INTO taxonomy_nodes (id, type, name, is_system) VALUES ($id, $type, $name, $isSystem)";
                    command.Parameters.AddWithValue("$id", id.ToString());
                    command.Parameters.AddWithValue("$type", (int)type);
                    command.Parameters.AddWithValue("$name", name.Trim());
                    command.Parameters.AddWithValue("$isSystem", isSystem ? 1 : 0);
                    command.ExecuteNonQuery();
                }
            }

            return id;
        }

        public void Rename(Guid id, string name)
        {
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE taxonomy_nodes SET name = $name WHERE id = $id";
                    command.Parameters.AddWithValue("$name", name.Trim());
                    command.Parameters.AddWithValue("$id", id.ToString());
                    command.ExecuteNonQuery();
                }
            }
        }

        public IReadOnlyList<Tag> ListTags()
        {
            var tags = new List<Tag>();
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT id, name FROM tags ORDER BY name";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tags.Add(Tag.Create(Guid.Parse(reader.GetString(0)), reader.GetString(1)));
                        }
                    }
                }
            }

            return tags;
        }

        public Guid EnsureTag(string name)
        {
            var trimmed = name.Trim();
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT id FROM tags WHERE name = $name";
                    command.Parameters.AddWithValue("$name", trimmed);
                    var existing = command.ExecuteScalar() as string;
                    if (existing != null)
                    {
                        return Guid.Parse(existing);
                    }
                }

                var id = Guid.NewGuid();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO tags (id, name) VALUES ($id, $name)";
                    command.Parameters.AddWithValue("$id", id.ToString());
                    command.Parameters.AddWithValue("$name", trimmed);
                    command.ExecuteNonQuery();
                }

                return id;
            }
        }
    }
}
