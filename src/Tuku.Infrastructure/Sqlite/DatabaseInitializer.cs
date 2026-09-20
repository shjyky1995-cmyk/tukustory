namespace Tuku.Infrastructure.Sqlite
{
    using System.Collections.Generic;
    using Tuku.Infrastructure.Sqlite.Migrations;

    public sealed class DatabaseInitializer
    {
        private readonly SqliteConnectionFactory connectionFactory;
        private readonly MigrationRunner migrationRunner;
        private readonly TaxonomySeeder taxonomySeeder;

        public DatabaseInitializer(string databasePath)
        {
            connectionFactory = new SqliteConnectionFactory(databasePath);
            migrationRunner = new MigrationRunner(connectionFactory, BuildMigrations());
            taxonomySeeder = new TaxonomySeeder(connectionFactory);
        }

        public SqliteConnectionFactory ConnectionFactory
        {
            get { return connectionFactory; }
        }

        public IReadOnlyList<int> Initialize()
        {
            var applied = migrationRunner.ApplyPending();
            taxonomySeeder.EnsureDefaults();
            return applied;
        }

        private static IEnumerable<Migration> BuildMigrations()
        {
            return new[] { Migration001_InitialSchema.Create() };
        }
    }
}
