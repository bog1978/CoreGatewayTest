DROP TABLE IF EXISTS file_to_process CASCADE;
DROP TABLE IF EXISTS file_to_process_history CASCADE;

CREATE TABLE file_to_process
(
    id         UUID        NOT NULL PRIMARY KEY,
    name       TEXT        NOT NULL UNIQUE,
    status     VARCHAR(10) NOT NULL CHECK ( status IN ('waiting', 'ok', 'error') ),
    try_count  INTEGER     NOT NULL,
    created_at TIMESTAMP   NOT NULL,
    try_after  TIMESTAMP   NOT NULL,
    errors     TEXT[]      NOT NULL
);

CREATE TABLE file_to_process_history
(
    id           UUID        NOT NULL,
    name         TEXT        NOT NULL,
    status       VARCHAR(10) NOT NULL,
    try_count    INTEGER     NOT NULL,
    created_at   TIMESTAMP   NOT NULL,
    completed_at TIMESTAMP   NOT NULL,
    errors       TEXT[]      NOT NULL
);

ALTER TABLE file_to_process
    OWNER TO postgres;