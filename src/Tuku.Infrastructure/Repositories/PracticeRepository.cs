namespace Tuku.Infrastructure.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Microsoft.Data.Sqlite;
    using Newtonsoft.Json;
    using Tuku.Application.Abstractions;
    using Tuku.Application.Dtos;
    using Tuku.Application.Search;
    using Tuku.Domain.Common;
    using Tuku.Domain.Entities;
    using Tuku.Domain.Search;
    using Tuku.Domain.ValueObjects;
    using Tuku.Infrastructure.Sqlite;

    public sealed class PracticeRepository : IPracticeRepository
    {
        private readonly SqliteConnectionFactory connectionFactory;

        public PracticeRepository(SqliteConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public PagedResult<PracticeListItem> Query(PracticeQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var terms = SplitTerms(query.Keywords);
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? 50 : query.PageSize;
            var tagIds = query.TagIds ?? new Guid[0];

            using (var connection = connectionFactory.CreateOpenConnection())
            {
                var where = new StringBuilder();
                where.Append(query.IncludeArchived ? "1 = 1" : "p.is_archived = 0");

                var parameters = new List<SqliteParameter>();
                if (query.RegionId.HasValue)
                {
                    where.Append(" AND a.region_id = $regionId");
                    parameters.Add(new SqliteParameter("$regionId", query.RegionId.Value.ToString()));
                }
                if (query.PartId.HasValue)
                {
                    where.Append(" AND p.main_part_id = $partId");
                    parameters.Add(new SqliteParameter("$partId", query.PartId.Value.ToString()));
                }
                if (query.AtlasId.HasValue)
                {
                    where.Append(" AND p.atlas_id = $atlasId");
                    parameters.Add(new SqliteParameter("$atlasId", query.AtlasId.Value.ToString()));
                }
                if (tagIds.Count > 0)
                {
                    var names = new List<string>();
                    for (var i = 0; i < tagIds.Count; i++)
                    {
                        var name = "$tag" + i.ToString(CultureInfo.InvariantCulture);
                        names.Add(name);
                        parameters.Add(new SqliteParameter(name, tagIds[i].ToString()));
                    }

                    where.Append(" AND (SELECT COUNT(*) FROM practice_tags pt WHERE pt.practice_id = p.id AND pt.tag_id IN ("
                        + string.Join(", ", names) + ")) = " + tagIds.Count.ToString(CultureInfo.InvariantCulture));
                }

                for (var i = 0; i < terms.Count; i++)
                {
                    var name = "$term" + i.ToString(CultureInfo.InvariantCulture);
                    parameters.Add(new SqliteParameter(name, "%" + EscapeLike(terms[i]) + "%"));
                    where.Append(" AND (s.name_norm LIKE " + name + " ESCAPE '\\'"
                        + " OR s.code_norm LIKE " + name + " ESCAPE '\\'"
                        + " OR s.body_norm LIKE " + name + " ESCAPE '\\'"
                        + " OR s.notes_norm LIKE " + name + " ESCAPE '\\'"
                        + " OR s.ref_norm LIKE " + name + " ESCAPE '\\'"
                        + " OR s.part_norm LIKE " + name + " ESCAPE '\\'"
                        + " OR s.atlas_name_norm LIKE " + name + " ESCAPE '\\'"
                        + " OR s.atlas_code_norm LIKE " + name + " ESCAPE '\\'"
                        + " OR s.tags_norm LIKE " + name + " ESCAPE '\\')");
                }

                var baseSql =
                    "FROM practices p"
                    + " JOIN search_documents s ON s.practice_id = p.id"
                    + " LEFT JOIN taxonomy_nodes part ON part.id = p.main_part_id"
                    + " LEFT JOIN atlases a ON a.id = p.atlas_id"
                    + " WHERE " + where;

                var total = ExecuteCount(connection, "SELECT COUNT(*) " + baseSql, parameters);

                var rows = new List<Tuple<PracticeListItem, PracticeSearchFields>>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT p.id, p.name, p.code, p.is_verified, p.is_archived, p.current_revision,"
                        + " part.name, a.name, s.name_norm, s.code_norm, s.body_norm, s.notes_norm, s.ref_norm,"
                        + " s.part_norm, s.atlas_name_norm, s.atlas_code_norm, s.tags_norm " + baseSql;
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new PracticeListItem
                            {
                                Id = Guid.Parse(reader.GetString(0)),
                                Name = reader.GetString(1),
                                Code = reader.IsDBNull(2) ? null : reader.GetString(2),
                                IsVerified = reader.GetInt32(3) != 0,
                                IsArchived = reader.GetInt32(4) != 0,
                                CurrentRevision = reader.GetInt32(5),
                                PartName = reader.IsDBNull(6) ? null : reader.GetString(6),
                                AtlasName = reader.IsDBNull(7) ? null : reader.GetString(7)
                            };
                            var fields = ReadSearchFields(reader, 8);
                            rows.Add(Tuple.Create(item, fields));
                        }
                    }
                }

                var ranked = rows
                    .Select(r =>
                    {
                        r.Item1.RankScore = SearchRanker.Score(r.Item2, terms);
                        return r.Item1;
                    })
                    .OrderByDescending(i => i.RankScore)
                    .ThenBy(i => i.Name, StringComparer.Ordinal)
                    .ThenBy(i => i.Code ?? string.Empty, StringComparer.Ordinal)
                    .ThenBy(i => i.Id.ToString())
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new PagedResult<PracticeListItem>(ranked, total, page, pageSize);
            }
        }

        public PracticeDetail GetDetail(Guid practiceId)
        {
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                PracticeDetail detail;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT p.id, p.atlas_id, p.name, p.code, p.main_part_id, p.notes, p.reference_note,"
                        + " p.is_verified, p.is_archived, p.current_revision, part.name, a.name"
                        + " FROM practices p"
                        + " LEFT JOIN taxonomy_nodes part ON part.id = p.main_part_id"
                        + " LEFT JOIN atlases a ON a.id = p.atlas_id"
                        + " WHERE p.id = $id";
                    command.Parameters.AddWithValue("$id", practiceId.ToString());
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }

                        detail = new PracticeDetail
                        {
                            Id = Guid.Parse(reader.GetString(0)),
                            AtlasId = reader.IsDBNull(1) ? (Guid?)null : Guid.Parse(reader.GetString(1)),
                            Name = reader.GetString(2),
                            Code = reader.IsDBNull(3) ? null : reader.GetString(3),
                            MainPartId = Guid.Parse(reader.GetString(4)),
                            Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                            ReferenceNote = reader.IsDBNull(6) ? null : reader.GetString(6),
                            IsVerified = reader.GetInt32(7) != 0,
                            IsArchived = reader.GetInt32(8) != 0,
                            CurrentRevision = reader.GetInt32(9),
                            PartName = reader.IsDBNull(10) ? null : reader.GetString(10),
                            AtlasName = reader.IsDBNull(11) ? null : reader.GetString(11)
                        };
                    }
                }

                detail.Layers = LoadLayers(connection, practiceId);
                detail.Sources = LoadSources(connection, practiceId);
                detail.TagNames = LoadTagNames(connection, practiceId);
                detail.Revisions = LoadRevisionSummaries(connection, practiceId);
                return detail;
            }
        }

        public IReadOnlyList<RevisionSummary> GetRevisions(Guid practiceId)
        {
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                return LoadRevisionSummaries(connection, practiceId);
            }
        }

        public PracticeSaveResult Save(PracticeEditCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            using (var connection = connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var state = LoadState(connection, transaction, command.PracticeId);
                if (state == null)
                {
                    return PracticeSaveResult.Invalid(new[] { "做法不存在" });
                }

                if (state.CurrentRevision != command.ExpectedRevision)
                {
                    return PracticeSaveResult.Conflict(state.CurrentRevision);
                }

                var practice = Practice.Load(state);
                var result = practice.UpdateContent(
                    command.ExpectedRevision,
                    command.Name,
                    command.Code,
                    command.MainPartId,
                    command.Notes,
                    command.ReferenceNote,
                    MapLayers(command.Layers),
                    MapSources(command.Sources),
                    ResolveTagIds(connection, transaction, command.TagNames),
                    command.Reason,
                    DateTime.UtcNow);
                if (!result.IsValid)
                {
                    return PracticeSaveResult.Invalid(result.Errors);
                }

                PersistPractice(connection, transaction, practice, command.Reason);
                transaction.Commit();
                return PracticeSaveResult.Ok(practice.CurrentRevision);
            }
        }

        public PracticeSaveResult RestoreRevision(Guid practiceId, int revisionNumber, int expectedRevision, string reason)
        {
            using (var connection = connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var state = LoadState(connection, transaction, practiceId);
                if (state == null)
                {
                    return PracticeSaveResult.Invalid(new[] { "做法不存在" });
                }

                if (state.CurrentRevision != expectedRevision)
                {
                    return PracticeSaveResult.Conflict(state.CurrentRevision);
                }

                var snapshot = LoadSnapshot(connection, transaction, practiceId, revisionNumber);
                if (snapshot == null)
                {
                    return PracticeSaveResult.Invalid(new[] { "修订不存在" });
                }

                var practice = Practice.Load(state);
                practice.RestoreFromSnapshot(snapshot, expectedRevision, DateTime.UtcNow);
                PersistPractice(connection, transaction, practice, reason ?? ("恢复修订 " + revisionNumber.ToString(CultureInfo.InvariantCulture)));
                transaction.Commit();
                return PracticeSaveResult.Ok(practice.CurrentRevision);
            }
        }

        public PracticeSaveResult SetVerified(Guid practiceId, bool verified)
        {
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE practices SET is_verified = $verified WHERE id = $id";
                    command.Parameters.AddWithValue("$verified", verified ? 1 : 0);
                    command.Parameters.AddWithValue("$id", practiceId.ToString());
                    var affected = command.ExecuteNonQuery();
                    if (affected == 0)
                    {
                        return PracticeSaveResult.Invalid(new[] { "做法不存在" });
                    }
                }

                return PracticeSaveResult.Ok(0);
            }
        }

        public Guid CreatePractice(CreatePracticeCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            var practiceId = Guid.NewGuid();
            using (var connection = connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var result = Practice.TryCreate(
                    practiceId,
                    command.AtlasId,
                    command.Name,
                    command.Code,
                    command.MainPartId,
                    command.Notes,
                    command.ReferenceNote,
                    MapLayers(command.Layers),
                    MapSources(command.Sources),
                    ResolveTagIds(connection, transaction, command.TagNames),
                    DateTime.UtcNow,
                    out var practice);
                if (!result.IsValid)
                {
                    throw new InvalidOperationException(string.Join("；", result.Errors));
                }

                PersistPractice(connection, transaction, practice, "新建做法");
                transaction.Commit();
                return practiceId;
            }
        }

        public RevisionSnapshotDto GetRevisionSnapshot(Guid practiceId, int revisionNumber)
        {
            using (var connection = connectionFactory.CreateOpenConnection())
            {
                string json = null;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT snapshot_json FROM practice_revisions WHERE practice_id = $id AND revision_no = $revision";
                    command.Parameters.AddWithValue("$id", practiceId.ToString());
                    command.Parameters.AddWithValue("$revision", revisionNumber);
                    json = command.ExecuteScalar() as string;
                }

                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                var snapshot = JsonConvert.DeserializeObject<PracticeSnapshot>(json);
                return new RevisionSnapshotDto
                {
                    RevisionNumber = revisionNumber,
                    Name = snapshot.Name,
                    Code = snapshot.Code,
                    Notes = snapshot.Notes,
                    ReferenceNote = snapshot.ReferenceNote,
                    Layers = snapshot.Layers
                        .Select((l, index) => new LayerDto
                        {
                            Id = l.LayerId,
                            Order = index + 1,
                            OriginalText = l.OriginalText,
                            CurrentText = l.CurrentText
                        })
                        .ToList(),
                    TagIds = snapshot.TagIds
                };
            }
        }

        private void PersistPractice(SqliteConnection connection, SqliteTransaction transaction, Practice practice, string reason)
        {
            var state = practice.CreateState();
            var now = DateTime.UtcNow.ToString("o");

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "UPDATE practices SET name = $name, code = $code, main_part_id = $part, notes = $notes,"
                    + " reference_note = $reference, is_verified = $verified, is_archived = $archived,"
                    + " current_revision = $revision, updated_utc = $updated WHERE id = $id";
                command.Parameters.AddWithValue("$name", state.Name);
                command.Parameters.AddWithValue("$code", (object)state.Code ?? DBNull.Value);
                command.Parameters.AddWithValue("$part", state.MainPartId.ToString());
                command.Parameters.AddWithValue("$notes", (object)state.Notes ?? DBNull.Value);
                command.Parameters.AddWithValue("$reference", (object)state.ReferenceNote ?? DBNull.Value);
                command.Parameters.AddWithValue("$verified", state.IsVerified ? 1 : 0);
                command.Parameters.AddWithValue("$archived", state.IsArchived ? 1 : 0);
                command.Parameters.AddWithValue("$revision", state.CurrentRevision);
                command.Parameters.AddWithValue("$updated", now);
                command.Parameters.AddWithValue("$id", state.Id.ToString());
                var affected = command.ExecuteNonQuery();
                if (affected == 0)
                {
                    command.CommandText =
                        "INSERT INTO practices (id, atlas_id, name, code, main_part_id, notes, reference_note,"
                        + " is_verified, is_archived, current_revision, created_utc, updated_utc)"
                        + " VALUES ($id, $atlasId, $name, $code, $part, $notes, $reference, $verified, $archived,"
                        + " $revision, $created, $updated)";
                    command.Parameters.AddWithValue("$atlasId", state.AtlasId.HasValue ? (object)state.AtlasId.Value.ToString() : DBNull.Value);
                    command.Parameters.AddWithValue("$created", now);
                    command.ExecuteNonQuery();
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM practice_layers WHERE practice_id = $id";
                command.Parameters.AddWithValue("$id", practice.Id.ToString());
                command.ExecuteNonQuery();
            }

            var order = 1;
            foreach (var layer in state.Layers)
            {
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText =
                        "INSERT INTO practice_layers (id, practice_id, order_no, original_text, current_text)"
                        + " VALUES ($id, $practiceId, $order, $original, $current)";
                    command.Parameters.AddWithValue("$id", layer.LayerId.ToString());
                    command.Parameters.AddWithValue("$practiceId", practice.Id.ToString());
                    command.Parameters.AddWithValue("$order", order);
                    command.Parameters.AddWithValue("$original", (object)layer.OriginalText ?? DBNull.Value);
                    command.Parameters.AddWithValue("$current", (object)layer.CurrentText ?? DBNull.Value);
                    command.ExecuteNonQuery();
                }

                order++;
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM practice_sources WHERE practice_id = $id";
                command.Parameters.AddWithValue("$id", practice.Id.ToString());
                command.ExecuteNonQuery();
            }

            foreach (var source in state.Sources)
            {
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText =
                        "INSERT INTO practice_sources (id, practice_id, page_id, region_x, region_y, region_w, region_h, region_source, region_reliable)"
                        + " VALUES ($id, $practiceId, $pageId, $x, $y, $w, $h, $source, $reliable)";
                    command.Parameters.AddWithValue("$id", source.SourceId.ToString());
                    command.Parameters.AddWithValue("$practiceId", practice.Id.ToString());
                    command.Parameters.AddWithValue("$pageId", source.PageId.ToString());
                    var region = source.Region;
                    command.Parameters.AddWithValue("$x", region == null ? (object)DBNull.Value : region.X);
                    command.Parameters.AddWithValue("$y", region == null ? (object)DBNull.Value : region.Y);
                    command.Parameters.AddWithValue("$w", region == null ? (object)DBNull.Value : region.Width);
                    command.Parameters.AddWithValue("$h", region == null ? (object)DBNull.Value : region.Height);
                    command.Parameters.AddWithValue("$source", region == null ? 0 : (int)region.Source);
                    command.Parameters.AddWithValue("$reliable", region != null && region.IsReliable ? 1 : 0);
                    command.ExecuteNonQuery();
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM practice_tags WHERE practice_id = $id";
                command.Parameters.AddWithValue("$id", practice.Id.ToString());
                command.ExecuteNonQuery();
            }

            foreach (var tagId in state.TagIds)
            {
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText =
                        "INSERT OR IGNORE INTO practice_tags (practice_id, tag_id) VALUES ($practiceId, $tagId)";
                    command.Parameters.AddWithValue("$practiceId", practice.Id.ToString());
                    command.Parameters.AddWithValue("$tagId", tagId.ToString());
                    command.ExecuteNonQuery();
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "INSERT INTO practice_revisions (id, practice_id, revision_no, snapshot_json, reason, created_utc)"
                    + " VALUES ($id, $practiceId, $revision, $snapshot, $reason, $created)";
                command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("$practiceId", practice.Id.ToString());
                command.Parameters.AddWithValue("$revision", practice.CurrentRevision);
                command.Parameters.AddWithValue("$snapshot", JsonConvert.SerializeObject(practice.CreateSnapshot()));
                command.Parameters.AddWithValue("$reason", (object)reason ?? DBNull.Value);
                command.Parameters.AddWithValue("$created", now);
                command.ExecuteNonQuery();
            }

            UpsertSearchDocument(connection, transaction, practice);
        }

        private void UpsertSearchDocument(SqliteConnection connection, SqliteTransaction transaction, Practice practice)
        {
            string atlasName = null;
            string atlasCode = null;
            if (practice.AtlasId.HasValue)
            {
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "SELECT name, code FROM atlases WHERE id = $id";
                    command.Parameters.AddWithValue("$id", practice.AtlasId.Value.ToString());
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            atlasName = reader.GetString(0);
                            atlasCode = reader.IsDBNull(1) ? null : reader.GetString(1);
                        }
                    }
                }
            }

            string partName = null;
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT name FROM taxonomy_nodes WHERE id = $id";
                command.Parameters.AddWithValue("$id", practice.MainPartId.ToString());
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        partName = reader.GetString(0);
                    }
                }
            }

            var tagNamesById = new Dictionary<Guid, string>();
            foreach (var tagId in practice.TagIds)
            {
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "SELECT name FROM tags WHERE id = $id";
                    command.Parameters.AddWithValue("$id", tagId.ToString());
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            tagNamesById[tagId] = reader.GetString(0);
                        }
                    }
                }
            }

            var fields = PracticeSearchFields.FromPractice(
                practice,
                atlasName,
                atlasCode,
                partName,
                tagNamesById);

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "INSERT INTO search_documents (practice_id, name_norm, code_norm, body_norm, notes_norm, ref_norm,"
                    + " part_norm, atlas_name_norm, atlas_code_norm, tags_norm)"
                    + " VALUES ($id, $name, $code, $body, $notes, $ref, $part, $atlasName, $atlasCode, $tags)"
                    + " ON CONFLICT(practice_id) DO UPDATE SET name_norm = $name, code_norm = $code, body_norm = $body,"
                    + " notes_norm = $notes, ref_norm = $ref, part_norm = $part, atlas_name_norm = $atlasName,"
                    + " atlas_code_norm = $atlasCode, tags_norm = $tags";
                command.Parameters.AddWithValue("$id", practice.Id.ToString());
                command.Parameters.AddWithValue("$name", fields.NameNormalized);
                command.Parameters.AddWithValue("$code", fields.CodeNormalized);
                command.Parameters.AddWithValue("$body", fields.BodyNormalized);
                command.Parameters.AddWithValue("$notes", fields.NotesNormalized);
                command.Parameters.AddWithValue("$ref", fields.ReferenceNormalized);
                command.Parameters.AddWithValue("$part", fields.MainPartNameNormalized);
                command.Parameters.AddWithValue("$atlasName", fields.AtlasNameNormalized);
                command.Parameters.AddWithValue("$atlasCode", fields.AtlasCodeNormalized);
                command.Parameters.AddWithValue("$tags", string.Join(" ", fields.TagNamesNormalized));
                command.ExecuteNonQuery();
            }
        }

        private static Practice.PracticeState LoadState(SqliteConnection connection, SqliteTransaction transaction, Guid practiceId)
        {
            Practice.PracticeState state = null;
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "SELECT id, atlas_id, name, code, main_part_id, notes, reference_note, is_verified, is_archived,"
                    + " current_revision, created_utc, updated_utc FROM practices WHERE id = $id";
                command.Parameters.AddWithValue("$id", practiceId.ToString());
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        state = new Practice.PracticeState
                        {
                            Id = Guid.Parse(reader.GetString(0)),
                            AtlasId = reader.IsDBNull(1) ? (Guid?)null : Guid.Parse(reader.GetString(1)),
                            Name = reader.GetString(2),
                            Code = reader.IsDBNull(3) ? null : reader.GetString(3),
                            MainPartId = Guid.Parse(reader.GetString(4)),
                            Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                            ReferenceNote = reader.IsDBNull(6) ? null : reader.GetString(6),
                            IsVerified = reader.GetInt32(7) != 0,
                            IsArchived = reader.GetInt32(8) != 0,
                            CurrentRevision = reader.GetInt32(9),
                            CreatedUtc = DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                            UpdatedUtc = DateTime.Parse(reader.GetString(11), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                            Layers = new List<Practice.PracticeState.LayerState>(),
                            Sources = new List<Practice.PracticeState.SourceState>(),
                            TagIds = new List<Guid>()
                        };
                    }
                }
            }

            if (state == null)
            {
                return null;
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "SELECT id, order_no, original_text, current_text FROM practice_layers WHERE practice_id = $id ORDER BY order_no";
                command.Parameters.AddWithValue("$id", practiceId.ToString());
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        state.Layers.Add(Practice.PracticeState.LayerState.Create(
                            Guid.Parse(reader.GetString(0)),
                            reader.IsDBNull(2) ? null : reader.GetString(2),
                            reader.IsDBNull(3) ? null : reader.GetString(3)));
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "SELECT id, page_id, region_x, region_y, region_w, region_h, region_source, region_reliable"
                    + " FROM practice_sources WHERE practice_id = $id";
                command.Parameters.AddWithValue("$id", practiceId.ToString());
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        PageRegion region = null;
                        if (!reader.IsDBNull(2))
                        {
                            region = new PageRegion(
                                reader.GetDouble(2),
                                reader.GetDouble(3),
                                reader.GetDouble(4),
                                reader.GetDouble(5),
                                (RegionSource)reader.GetInt32(6),
                                reader.GetInt32(7) != 0);
                        }

                        state.Sources.Add(Practice.PracticeState.SourceState.Create(
                            Guid.Parse(reader.GetString(0)),
                            Guid.Parse(reader.GetString(1)),
                            region));
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT tag_id FROM practice_tags WHERE practice_id = $id";
                command.Parameters.AddWithValue("$id", practiceId.ToString());
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        state.TagIds.Add(Guid.Parse(reader.GetString(0)));
                    }
                }
            }

            return state;
        }

        private static PracticeSnapshot LoadSnapshot(SqliteConnection connection, SqliteTransaction transaction, Guid practiceId, int revisionNumber)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    "SELECT snapshot_json FROM practice_revisions WHERE practice_id = $id AND revision_no = $revision";
                command.Parameters.AddWithValue("$id", practiceId.ToString());
                command.Parameters.AddWithValue("$revision", revisionNumber);
                var json = command.ExecuteScalar() as string;
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<PracticeSnapshot>(json);
            }
        }

        private static List<LayerDto> LoadLayers(SqliteConnection connection, Guid practiceId)
        {
            var layers = new List<LayerDto>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT id, order_no, original_text, current_text FROM practice_layers WHERE practice_id = $id ORDER BY order_no";
                command.Parameters.AddWithValue("$id", practiceId.ToString());
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        layers.Add(new LayerDto
                        {
                            Id = Guid.Parse(reader.GetString(0)),
                            Order = reader.GetInt32(1),
                            OriginalText = reader.IsDBNull(2) ? null : reader.GetString(2),
                            CurrentText = reader.IsDBNull(3) ? null : reader.GetString(3)
                        });
                    }
                }
            }

            return layers;
        }

        private static List<SourceDto> LoadSources(SqliteConnection connection, Guid practiceId)
        {
            var sources = new List<SourceDto>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT s.id, s.page_id, pg.page_number, pg.printed_label, s.region_x, s.region_y, s.region_w, s.region_h, s.region_source, s.region_reliable"
                    + " FROM practice_sources s JOIN atlas_pages pg ON pg.id = s.page_id WHERE s.practice_id = $id";
                command.Parameters.AddWithValue("$id", practiceId.ToString());
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sources.Add(new SourceDto
                        {
                            Id = Guid.Parse(reader.GetString(0)),
                            PageId = Guid.Parse(reader.GetString(1)),
                            PageNumber = reader.GetInt32(2),
                            PrintedPageLabel = reader.IsDBNull(3) ? null : reader.GetString(3),
                            X = reader.IsDBNull(4) ? (double?)null : reader.GetDouble(4),
                            Y = reader.IsDBNull(5) ? (double?)null : reader.GetDouble(5),
                            Width = reader.IsDBNull(6) ? (double?)null : reader.GetDouble(6),
                            Height = reader.IsDBNull(7) ? (double?)null : reader.GetDouble(7),
                            RegionSource = ((RegionSource)reader.GetInt32(8)).ToString(),
                            RegionReliable = reader.GetInt32(9) != 0
                        });
                    }
                }
            }

            return sources;
        }

        private static List<string> LoadTagNames(SqliteConnection connection, Guid practiceId)
        {
            var names = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT t.name FROM practice_tags pt JOIN tags t ON t.id = pt.tag_id WHERE pt.practice_id = $id ORDER BY t.name";
                command.Parameters.AddWithValue("$id", practiceId.ToString());
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        names.Add(reader.GetString(0));
                    }
                }
            }

            return names;
        }

        private static List<RevisionSummary> LoadRevisionSummaries(SqliteConnection connection, Guid practiceId)
        {
            var revisions = new List<RevisionSummary>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT revision_no, reason, created_utc FROM practice_revisions WHERE practice_id = $id ORDER BY revision_no DESC";
                command.Parameters.AddWithValue("$id", practiceId.ToString());
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        revisions.Add(new RevisionSummary
                        {
                            RevisionNumber = reader.GetInt32(0),
                            Reason = reader.IsDBNull(1) ? null : reader.GetString(1),
                            CreatedUtc = DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)
                        });
                    }
                }
            }

            return revisions;
        }

        private static PracticeSearchFields ReadSearchFields(SqliteDataReader reader, int startIndex)
        {
            var tagsNorm = reader.GetString(startIndex + 8);
            var tags = new List<string>();
            if (!string.IsNullOrWhiteSpace(tagsNorm))
            {
                tags.AddRange(tagsNorm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }

            return PracticeSearchFields.FromStoredFields(
                reader.GetString(startIndex),
                reader.GetString(startIndex + 1),
                reader.GetString(startIndex + 2),
                reader.GetString(startIndex + 3),
                reader.GetString(startIndex + 4),
                tags,
                reader.GetString(startIndex + 6),
                reader.GetString(startIndex + 7),
                reader.GetString(startIndex + 5));
        }

        private static List<Guid> ResolveTagIds(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<string> tagNames)
        {
            var ids = new List<Guid>();
            if (tagNames == null)
            {
                return ids;
            }

            foreach (var name in tagNames.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).Distinct())
            {
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "SELECT id FROM tags WHERE name = $name";
                    command.Parameters.AddWithValue("$name", name);
                    var existing = command.ExecuteScalar() as string;
                    if (existing != null)
                    {
                        ids.Add(Guid.Parse(existing));
                        continue;
                    }
                }

                var id = Guid.NewGuid();
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "INSERT INTO tags (id, name) VALUES ($id, $name)";
                    command.Parameters.AddWithValue("$id", id.ToString());
                    command.Parameters.AddWithValue("$name", name);
                    command.ExecuteNonQuery();
                }

                ids.Add(id);
            }

            return ids;
        }

        private static IEnumerable<Practice.LayerInput> MapLayers(IEnumerable<PracticeEditCommand.LayerEdit> layers)
        {
            return (layers ?? Enumerable.Empty<PracticeEditCommand.LayerEdit>())
                .Select(l => new Practice.LayerInput(l.OriginalText, l.CurrentText));
        }

        private static IEnumerable<Practice.SourceInput> MapSources(IEnumerable<PracticeEditCommand.SourceEdit> sources)
        {
            return (sources ?? Enumerable.Empty<PracticeEditCommand.SourceEdit>())
                .Select(s => new Practice.SourceInput(
                    s.PageId,
                    s.X.HasValue && s.Y.HasValue && s.Width.HasValue && s.Height.HasValue
                        ? new PageRegion(s.X.Value, s.Y.Value, s.Width.Value, s.Height.Value, ParseRegionSource(s.RegionSource), s.RegionReliable)
                        : null));
        }

        private static RegionSource ParseRegionSource(string value)
        {
            RegionSource parsed;
            if (Enum.TryParse(value, true, out parsed))
            {
                return parsed;
            }

            return RegionSource.Unknown;
        }

        private static int ExecuteCount(SqliteConnection connection, string sql, List<SqliteParameter> parameters)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }

                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static List<string> SplitTerms(string keywords)
        {
            if (string.IsNullOrWhiteSpace(keywords))
            {
                return new List<string>();
            }

            return keywords
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(TextNormalizer.Normalize)
                .Where(t => t.Length > 0)
                .ToList();
        }

        private static string EscapeLike(string value)
        {
            return value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }
    }
}
