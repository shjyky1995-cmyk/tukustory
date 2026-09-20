namespace Tuku.Infrastructure.Sqlite
{
    using System;
    using System.IO;
    using Microsoft.Data.Sqlite;

    public sealed class SqliteConnectionFactory
    {
        private readonly string databasePath;

        public SqliteConnectionFactory(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("数据库路径不能为空", "databasePath");
            }

            this.databasePath = Path.GetFullPath(databasePath);
        }

        public string DatabasePath
        {
            get { return databasePath; }
        }

        public SqliteConnection CreateOpenConnection()
        {
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var connection = new SqliteConnection(BuildConnectionString());
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA foreign_keys = ON;";
                command.ExecuteNonQuery();
            }

            return connection;
        }

        public string BuildConnectionString()
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                ForeignKeys = true,
                DefaultTimeout = 30
            };
            return builder.ToString();
        }
    }
}
