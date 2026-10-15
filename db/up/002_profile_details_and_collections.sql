-- Profile details: location and summary. Skills move from a text[] column to their own table.
ALTER TABLE professionals
    ADD COLUMN IF NOT EXISTS location TEXT NULL,
    ADD COLUMN IF NOT EXISTS summary  TEXT NULL;

ALTER TABLE professionals DROP COLUMN IF EXISTS skills;

CREATE TABLE IF NOT EXISTS professional_photos (
    professional_id UUID PRIMARY KEY REFERENCES professionals (id) ON DELETE CASCADE,
    content_type    TEXT        NOT NULL,
    content         BYTEA       NOT NULL,
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS skills (
    id                  UUID PRIMARY KEY,
    professional_id     UUID        NOT NULL REFERENCES professionals (id) ON DELETE CASCADE,
    name                TEXT        NOT NULL,
    category            TEXT        NOT NULL,
    years_of_experience INT         NOT NULL CHECK (years_of_experience >= 0),
    level               SMALLINT    NOT NULL CHECK (level BETWEEN 1 AND 5),
    is_primary          BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_skills_professional_id ON skills (professional_id);

CREATE TABLE IF NOT EXISTS experiences (
    id              UUID PRIMARY KEY,
    professional_id UUID        NOT NULL REFERENCES professionals (id) ON DELETE CASCADE,
    company         TEXT        NOT NULL,
    role            TEXT        NOT NULL,
    start_date      DATE        NOT NULL,
    end_date        DATE        NULL,
    description     TEXT        NOT NULL DEFAULT '',
    technologies    TEXT[]      NOT NULL DEFAULT '{}',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    CHECK (end_date IS NULL OR end_date >= start_date)
);
CREATE INDEX IF NOT EXISTS ix_experiences_professional_id ON experiences (professional_id);

CREATE TABLE IF NOT EXISTS education (
    id              UUID PRIMARY KEY,
    professional_id UUID        NOT NULL REFERENCES professionals (id) ON DELETE CASCADE,
    institution     TEXT        NOT NULL,
    degree          TEXT        NOT NULL,
    start_year      INT         NOT NULL,
    end_year        INT         NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    CHECK (end_year IS NULL OR end_year >= start_year)
);
CREATE INDEX IF NOT EXISTS ix_education_professional_id ON education (professional_id);
