/*
powered by  : (c) 2025, Ramadan Ismael - All rights reserved!!
to          : ETC - Auth Service
*/

-- sudo -u postgres psql
-- CREATE DATABASE etc_db_auth_service;
-- GRANT ALL PRIVILEGES ON DATABASE etc_db_auth_service TO ramadan;

CREATE EXTENSION IF NOT EXISTS pgcrypto; -- auto uuid

-- REFRESH TOKEN
CREATE TABLE IF NOT EXISTS tbRefreshToken
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    refresh_token TEXT NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL DEFAULT (NOW() + INTERVAL '2 months'),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    revoked_at TIMESTAMPTZ
);
CREATE INDEX tbrefreshtoken_user_id ON tbRefreshToken(user_id);
CREATE INDEX tbrefreshtoken_token ON tbRefreshToken(refresh_token);
-- REFRESH TOKEN