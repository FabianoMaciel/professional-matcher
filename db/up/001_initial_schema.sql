CREATE EXTENSION IF NOT EXISTS citext;

CREATE TABLE IF NOT EXISTS professionals (
    id          UUID PRIMARY KEY,
    full_name   TEXT        NOT NULL,
    email       CITEXT      NOT NULL UNIQUE,
    headline    TEXT        NULL,
    skills      TEXT[]      NOT NULL DEFAULT '{}',
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);
