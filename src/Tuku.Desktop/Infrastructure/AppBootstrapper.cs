namespace Tuku.Desktop.Infrastructure
{
    using System;
    using System.IO;
    using Tuku.Application.Abstractions;
    using Tuku.Application.Services;
    using Tuku.Infrastructure.Repositories;
    using Tuku.Infrastructure.Sqlite;

    public static class AppBootstrapper
    {
        private static readonly object SyncRoot = new object();
        private static bool initialized;

        public static string DatabasePath { get; private set; } = null!;

        public static PracticeService PracticeService { get; private set; } = null!;

        public static AtlasService AtlasService { get; private set; } = null!;

        public static TaxonomyService TaxonomyService { get; private set; } = null!;

        public static void Initialize()
        {
            lock (SyncRoot)
            {
                if (initialized)
                {
                    return;
                }

                var root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Tuku",
                    "Library");
                Directory.CreateDirectory(root);
                DatabasePath = Path.Combine(root, "library.db");

                var initializer = new DatabaseInitializer(DatabasePath);
                initializer.Initialize();

                var connectionFactory = new SqliteConnectionFactory(DatabasePath);
                IPracticeRepository practiceRepository = new PracticeRepository(connectionFactory);
                PracticeService = new PracticeService(practiceRepository);
                AtlasService = new AtlasService(new AtlasRepository(connectionFactory));
                TaxonomyService = new TaxonomyService(new TaxonomyRepository(connectionFactory));
                initialized = true;
            }
        }
    }
}
