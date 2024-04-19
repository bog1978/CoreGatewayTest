DROP TABLE IF EXISTS file_to_process CASCADE;

CREATE TABLE IF NOT EXISTS file_to_process
(
    id           UUID      NOT NULL PRIMARY KEY,
    name         TEXT      NOT NULL,
    created_at   TIMESTAMP NOT NULL,
    completed_at TIMESTAMP,
    error        TEXT
);

ALTER TABLE file_to_process
    OWNER TO postgres;

CREATE INDEX IF NOT EXISTS file_to_process_index
    ON file_to_process (name, completed_at);