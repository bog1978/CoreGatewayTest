--CREATE DATABASE logic_storage;
DROP TABLE IF EXISTS file_data CASCADE;

CREATE TABLE file_data
(
    id   uuid PRIMARY KEY,
    name TEXT,
    data bytea
);