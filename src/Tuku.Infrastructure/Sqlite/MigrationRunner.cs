namespace Tuku.Infrastructure.Sqlite
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Data.Sqlite;

    public sealed class Migration
    {
        public Migration(int version, string name, IEnumerable<string> statements)
        {
            Version = version;
            Name = name;
            Statements = statements;
        }

        public int Version { get; private set; }

        public string Name { get; private set; }

        public IEnumerable<string> Statements { get; private set; }
    }

    public sealed class MigrationRunner
    {
        private readonly SqliteConnectionFactory connectionFactory;
        private readonly IEnumerable<Migration> migrations;

        public MigrationRunner(SqliteConnectionFactory connectionFactory, IEnumerable<Migration> migrations)
        {
            this.connectionFactory = connectionFactory;
            this.migrations = migrations;
        }

        public IReadOnlyList<int> ApplyPending()
        {
            var applied = new List<int>();
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                EnsureVersionTable(connection);
                var appliedVersions = ReadAppliedVersions(connection);
                foreach (var migration in migrations)
                {
                    if (appliedVersions.Contains(migration.Version))
                    {
                        continue;
                    }

                    using (var transaction = connection.BeginTransaction())
                    {
                        foreach (var statement in migration.Statements)
                        {
                            using (var command = connection.CreateCommand())
                            {
                                command.Transaction = transaction;
                                command.CommandText = statement;
                                command.ExecuteNonQuery();
                            }
                        }

                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText =
                                "INSERT INTO schema_migrations (version, name, applied_utc) VALUES ($version, $name, $appliedUtc)";
                            command.Parameters.AddWithValue("$version", migration.Version);
                            command.Parameters.AddWithValue("$name", migration.Name);
                            command.Parameters.AddWithValue("$appliedUtc", DateTime.UtcNow.ToString("o"));
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }

                    applied.Add(migration.Version);
                }
            }

            return applied;
        }

        public IReadOnlyList<int> GetAppliedVersions()
        {
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                EnsureVersionTable(connection);
                return ReadAppliedVersions(connection);
            }
        }

        private static void EnsureVersionTable(SqliteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "CREATE TABLE IF NOT EXISTS schema_migrations (version INTEGER PRIMARY KEY, name TEXT NOT NULL, applied_utc TEXT NOT NULL)";
                command.ExecuteNonQuery();
            }
        }

        private static List<int> ReadAppliedVersions(SqliteConnection connection)
        {
            var versions = new List<int>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT version FROM schema_migrations ORDER BY version";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        versions.Add(reader.GetInt32(0));
                    }
                }
            }

            return versions;
        }
    }
}
