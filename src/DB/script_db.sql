/*
powered by  : (c) 2025, Ramadan Ismael - All rights reserved!!
to          : ETC - User Service
*/

-- sudo -u postgres psql
-- CREATE DATABASE etc_db_auth_service;
-- GRANT ALL PRIVILEGES ON DATABASE etc_db_auth_service TO ramadan;

CREATE EXTENSION IF NOT EXISTS pgcrypto; -- auto uuid

-- REFRESH TOKEN
CREATE TABLE IF NOT EXISTS tbRefreshToken
(
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    token TEXT NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    revoked_at TIMESTAMPTZ
);
-- REFRESH TOKEN

-- QUERIES
DROP TABLE tbRefreshToken;
SELECT * FROM tbRefreshToken;
-- QUERIES