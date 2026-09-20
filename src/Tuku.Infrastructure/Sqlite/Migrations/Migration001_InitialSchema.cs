namespace Tuku.Infrastructure.Sqlite.Migrations
{
    using System.Collections.Generic;

    public static class Migration001_InitialSchema
    {
        public const int Version = 1;

        public static Migration Create()
        {
            return new Migration(Version, "initial_schema", Statements());
        }

        private static IEnumerable<string> Statements()
        {
            var statements = new List<string>();

            statements.Add(@"
CREATE TABLE taxonomy_nodes (
    id TEXT PRIMARY KEY,
    type INTEGER NOT NULL,
    name TEXT NOT NULL,
    is_system INTEGER NOT NULL DEFAULT 0,
    UNIQUE (type, name)
)");

            statements.Add(@"
CREATE TABLE tags (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL UNIQUE
)");

            statements.Add(@"
CREATE TABLE atlases (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    code TEXT,
    region_id TEXT NOT NULL REFERENCES taxonomy_nodes(id),
    year_note TEXT,
    fingerprint TEXT NOT NULL,
    managed_path TEXT NOT NULL,
    is_archived INTEGER NOT NULL DEFAULT 0,
    created_utc TEXT NOT NULL
)");

            statements.Add("CREATE INDEX ix_atlases_region ON atlases (region_id)");
            statements.Add("CREATE UNIQUE INDEX ix_atlases_fingerprint ON atlases (fingerprint)");

            statements.Add(@"
CREATE TABLE atlas_pages (
    id TEXT PRIMARY KEY,
    atlas_id TEXT NOT NULL REFERENCES atlases(id),
    page_number INTEGER NOT NULL,
    printed_label TEXT,
    width_pt REAL NOT NULL DEFAULT 0,
    height_pt REAL NOT NULL DEFAULT 0,
    image_cache_key TEXT,
    UNIQUE (atlas_id, page_number)
)");

            statements.Add(@"
CREATE TABLE practices (
    id TEXT PRIMARY KEY,
    atlas_id TEXT REFERENCES atlases(id),
    name TEXT NOT NULL,
    code TEXT,
    main_part_id TEXT NOT NULL REFERENCES taxonomy_nodes(id),
    notes TEXT,
    reference_note TEXT,
    is_verified INTEGER NOT NULL DEFAULT 0,
    is_archived INTEGER NOT NULL DEFAULT 0,
    current_revision INTEGER NOT NULL DEFAULT 1,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL
)");

            statements.Add("CREATE INDEX ix_practices_atlas ON practices (atlas_id)");
            statements.Add("CREATE INDEX ix_practices_part ON practices (main_part_id)");
            statements.Add("CREATE INDEX ix_practices_name ON practices (name)");
            statements.Add("CREATE INDEX ix_practices_code ON practices (code)");

            statements.Add(@"
CREATE TABLE practice_layers (
    id TEXT PRIMARY KEY,
    practice_id TEXT NOT NULL REFERENCES practices(id) ON DELETE CASCADE,
    order_no INTEGER NOT NULL,
    original_text TEXT,
    current_text TEXT,
    UNIQUE (practice_id, order_no)
)");

            statements.Add(@"
CREATE TABLE practice_sources (
    id TEXT PRIMARY KEY,
    practice_id TEXT NOT NULL REFERENCES practices(id) ON DELETE CASCADE,
    page_id TEXT NOT NULL REFERENCES atlas_pages(id),
    region_x REAL,
    region_y REAL,
    region_w REAL,
    region_h REAL,
    region_source INTEGER NOT NULL DEFAULT 0,
    region_reliable INTEGER NOT NULL DEFAULT 0
)");

            statements.Add("CREATE INDEX ix_practice_sources_page ON practice_sources (page_id)");

            statements.Add(@"
CREATE TABLE practice_revisions (
    id TEXT PRIMARY KEY,
    practice_id TEXT NOT NULL REFERENCES practices(id) ON DELETE CASCADE,
    revision_no INTEGER NOT NULL,
    snapshot_json TEXT NOT NULL,
    reason TEXT,
    created_utc TEXT NOT NULL,
    UNIQUE (practice_id, revision_no)
)");

            statements.Add(@"
CREATE TABLE practice_tags (
    practice_id TEXT NOT NULL REFERENCES practices(id) ON DELETE CASCADE,
    tag_id TEXT NOT NULL REFERENCES tags(id),
    PRIMARY KEY (practice_id, tag_id)
)");

            statements.Add(@"
CREATE TABLE search_documents (
    practice_id TEXT PRIMARY KEY REFERENCES practices(id) ON DELETE CASCADE,
    name_norm TEXT NOT NULL DEFAULT '',
    code_norm TEXT NOT NULL DEFAULT '',
    body_norm TEXT NOT NULL DEFAULT '',
    notes_norm TEXT NOT NULL DEFAULT '',
    ref_norm TEXT NOT NULL DEFAULT '',
    part_norm TEXT NOT NULL DEFAULT '',
    atlas_name_norm TEXT NOT NULL DEFAULT '',
    atlas_code_norm TEXT NOT NULL DEFAULT '',
    tags_norm TEXT NOT NULL DEFAULT ''
)");

            statements.Add(@"
CREATE TABLE recognition_runs (
    id TEXT PRIMARY KEY,
    input_fingerprint TEXT NOT NULL,
    config_fingerprint TEXT NOT NULL,
    provider TEXT NOT NULL,
    model TEXT,
    status INTEGER NOT NULL,
    raw_path TEXT,
    provider_request_id TEXT,
    usage_json TEXT,
    created_utc TEXT NOT NULL
)");

            statements.Add("CREATE INDEX ix_recognition_runs_cache ON recognition_runs (input_fingerprint, config_fingerprint)");

            statements.Add(@"
CREATE TABLE recognition_candidates (
    id TEXT PRIMARY KEY,
    run_id TEXT NOT NULL REFERENCES recognition_runs(id) ON DELETE CASCADE,
    practice_id TEXT REFERENCES practices(id) ON DELETE SET NULL,
    candidate_json TEXT NOT NULL,
    problems_json TEXT,
    adopted INTEGER NOT NULL DEFAULT 0,
    created_utc TEXT NOT NULL
)");

            statements.Add(@"
CREATE TABLE import_jobs (
    id TEXT PRIMARY KEY,
    atlas_id TEXT REFERENCES atlases(id) ON DELETE CASCADE,
    page_from INTEGER,
    page_to INTEGER,
    config_json TEXT,
    status INTEGER NOT NULL,
    retry_count INTEGER NOT NULL DEFAULT 0,
    error TEXT,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL
)");

            statements.Add(@"
CREATE TABLE import_page_tasks (
    id TEXT PRIMARY KEY,
    job_id TEXT NOT NULL REFERENCES import_jobs(id) ON DELETE CASCADE,
    page_number INTEGER NOT NULL,
    status INTEGER NOT NULL,
    retry_count INTEGER NOT NULL DEFAULT 0,
    error TEXT,
    updated_utc TEXT NOT NULL,
    UNIQUE (job_id, page_number)
)");

            statements.Add(@"
CREATE TABLE insertion_presets (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    style_name TEXT,
    text_height REAL NOT NULL,
    width REAL NOT NULL,
    spacing REAL NOT NULL,
    layer_name TEXT,
    is_default INTEGER NOT NULL DEFAULT 0
)");

            statements.Add(@"
CREATE TABLE cad_requests (
    id TEXT PRIMARY KEY,
    practice_id TEXT NOT NULL REFERENCES practices(id) ON DELETE CASCADE,
    revision INTEGER NOT NULL,
    target_instance TEXT,
    target_document TEXT,
    request_json TEXT NOT NULL,
    status INTEGER NOT NULL,
    result_json TEXT,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL
)");

            return statements;
        }
    }
}
