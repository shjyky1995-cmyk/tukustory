namespace Tuku.Infrastructure.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.Data.Sqlite;
    using Tuku.Application.Abstractions;
    using Tuku.Domain.Common;
    using Tuku.Domain.Entities;
    using Tuku.Infrastructure.Sqlite;

    public sealed class AtlasRepository : IAtlasRepository
    {
        private readonly SqliteConnectionFactory connectionFactory;

        public AtlasRepository(SqliteConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public Guid CreateAtlas(
            string name,
            string code,
            Guid regionId,
            string yearOrVersionNote,
            string originalFileFingerprint,
            string managedFilePath)
        {
            var id = Guid.NewGuid();
            var now = DateTime.UtcNow.ToString("o");
            var result = Atlas.TryCreate(
                id,
                name,
                code,
                regionId,
                yearOrVersionNote,
                originalFileFingerprint,
                managedFilePath,
                out var atlas);
            if (!result.IsValid)
            {
                throw new InvalidOperationException(string.Join("；", result.Errors));
            }

            using (var connection = connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "INSERT INTO atlases (id, name, code, region_id, year_note, fingerprint, managed_path, is_archived, created_utc)"
                        + " VALUES ($id, $name, $code, $region, $year, $fingerprint, $path, 0, $created)";
                    command.Parameters.AddWithValue("$id", id.ToString());
                    command.Parameters.AddWithValue("$name", atlas.Name);
                    command.Parameters.AddWithValue("$code", (object)atlas.Code ?? DBNull.Value);
                    command.Parameters.AddWithValue("$region", regionId.ToString());
                    command.Parameters.AddWithValue("$year", (object)atlas.YearOrVersionNote ?? DBNull.Value);
                    command.Parameters.AddWithValue("$fingerprint", originalFileFingerprint);
                    command.Parameters.AddWithValue("$path", managedFilePath);
                    command.Parameters.AddWithValue("$created", now);
                    command.ExecuteNonQuery();
                }
            }

            return id;
        }

        public AtlasSummary GetAtlas(Guid atlasId)
        {
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT a.id, a.name, a.code, a.region_id, r.name, a.year_note, a.is_archived,"
                        + " (SELECT COUNT(*) FROM practices p WHERE p.atlas_id = a.id AND p.is_archived = 0)"
                        + " FROM atlases a JOIN taxonomy_nodes r ON r.id = a.region_id WHERE a.id = $id";
                    command.Parameters.AddWithValue("$id", atlasId.ToString());
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }

                        return new AtlasSummary
                        {
                            Id = Guid.Parse(reader.GetString(0)),
                            Name = reader.GetString(1),
                            Code = reader.IsDBNull(2) ? null : reader.GetString(2),
                            RegionId = Guid.Parse(reader.GetString(3)),
                            RegionName = reader.GetString(4),
                            YearOrVersionNote = reader.IsDBNull(5) ? null : reader.GetString(5),
                            IsArchived = reader.GetInt32(6) != 0,
                            PracticeCount = reader.GetInt32(7)
                        };
                    }
                }
            }
        }

        public IReadOnlyList<AtlasSummary> ListAtlases(bool includeArchived)
        {
            var atlases = new List<AtlasSummary>();
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT a.id, a.name, a.code, a.region_id, r.name, a.year_note, a.is_archived,"
                        + " (SELECT COUNT(*) FROM practices p WHERE p.atlas_id = a.id AND p.is_archived = 0)"
                        + " FROM atlases a JOIN taxonomy_nodes r ON r.id = a.region_id"
                        + (includeArchived ? string.Empty : " WHERE a.is_archived = 0")
                        + " ORDER BY a.name";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            atlases.Add(new AtlasSummary
                            {
                                Id = Guid.Parse(reader.GetString(0)),
                                Name = reader.GetString(1),
                                Code = reader.IsDBNull(2) ? null : reader.GetString(2),
                                RegionId = Guid.Parse(reader.GetString(3)),
                                RegionName = reader.GetString(4),
                                YearOrVersionNote = reader.IsDBNull(5) ? null : reader.GetString(5),
                                IsArchived = reader.GetInt32(6) != 0,
                                PracticeCount = reader.GetInt32(7)
                            });
                        }
                    }
                }
            }

            return atlases;
        }

        public void RegisterPage(
            Guid atlasId,
            int pageNumber,
            string printedPageLabel,
            double widthPoints,
            double heightPoints,
            string imageCacheKey)
        {
            var pageId = Guid.NewGuid();
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "INSERT OR IGNORE INTO atlas_pages (id, atlas_id, page_number, printed_label, width_pt, height_pt, image_cache_key)"
                        + " VALUES ($id, $atlasId, $pageNumber, $label, $width, $height, $cacheKey)";
                    command.Parameters.AddWithValue("$id", pageId.ToString());
                    command.Parameters.AddWithValue("$atlasId", atlasId.ToString());
                    command.Parameters.AddWithValue("$pageNumber", pageNumber);
                    command.Parameters.AddWithValue("$label", (object)printedPageLabel ?? DBNull.Value);
                    command.Parameters.AddWithValue("$width", widthPoints);
                    command.Parameters.AddWithValue("$height", heightPoints);
                    command.Parameters.AddWithValue("$cacheKey", (object)imageCacheKey ?? DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public IReadOnlyList<AtlasPageSummary> GetPages(Guid atlasId)
        {
            var pages = new List<AtlasPageSummary>();
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT id, page_number, printed_label, width_pt, height_pt FROM atlas_pages WHERE atlas_id = $atlasId ORDER BY page_number";
                    command.Parameters.AddWithValue("$atlasId", atlasId.ToString());
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            pages.Add(new AtlasPageSummary
                            {
                                Id = Guid.Parse(reader.GetString(0)),
                                PageNumber = reader.GetInt32(1),
                                PrintedPageLabel = reader.IsDBNull(2) ? null : reader.GetString(2),
                                WidthPoints = reader.GetDouble(3),
                                HeightPoints = reader.GetDouble(4)
                            });
                        }
                    }
                }
            }

            return pages;
        }

        public void ArchiveAtlas(Guid atlasId)
        {
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE atlases SET is_archived = 1 WHERE id = $id";
                    command.Parameters.AddWithValue("$id", atlasId.ToString());
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
