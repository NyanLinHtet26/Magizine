-- ============================================================================
-- SCHEMA MIGRATION: Partial Unique Indexes for Soft-Delete Tables
-- ============================================================================
-- This script replaces inline UNIQUE constraints with partial unique indexes
-- on 5 tables that have soft-delete columns (IsDeleted). This allows a value
-- (username, email, slug, name) to be reused after the owning row is soft-deleted.
--
-- Run this in Supabase SQL Editor BEFORE re-scaffolding with Doc\Rescaffold.ps1
-- ============================================================================

BEGIN;

-- 1. Tbl_Admin: drop inline UNIQUE constraints, create partial indexes
-- Find the auto-generated constraint names first:
-- SELECT conname FROM pg_constraint WHERE conrelid = 'Tbl_Admin'::regclass AND contype = 'u';

ALTER TABLE "Tbl_Admin" DROP CONSTRAINT IF EXISTS "Tbl_Admin_Username_key";
ALTER TABLE "Tbl_Admin" DROP CONSTRAINT IF EXISTS "Tbl_Admin_Email_key";

CREATE UNIQUE INDEX "UX_Tbl_Admin_Username_NotDeleted"
    ON "Tbl_Admin" ("Username")
    WHERE "IsDeleted" = false;

CREATE UNIQUE INDEX "UX_Tbl_Admin_Email_NotDeleted"
    ON "Tbl_Admin" ("Email")
    WHERE "IsDeleted" = false;

-- 2. Tbl_Author: drop inline UNIQUE constraints, create partial indexes
ALTER TABLE "Tbl_Author" DROP CONSTRAINT IF EXISTS "Tbl_Author_Email_key";
ALTER TABLE "Tbl_Author" DROP CONSTRAINT IF EXISTS "Tbl_Author_Slug_key";

CREATE UNIQUE INDEX "UX_Tbl_Author_Email_NotDeleted"
    ON "Tbl_Author" ("Email")
    WHERE "IsDeleted" = false;

CREATE UNIQUE INDEX "UX_Tbl_Author_Slug_NotDeleted"
    ON "Tbl_Author" ("Slug")
    WHERE "IsDeleted" = false;

-- 3. Tbl_ArticleCategory: drop inline UNIQUE constraints, create partial indexes
ALTER TABLE "Tbl_ArticleCategory" DROP CONSTRAINT IF EXISTS "Tbl_ArticleCategory_Name_key";
ALTER TABLE "Tbl_ArticleCategory" DROP CONSTRAINT IF EXISTS "Tbl_ArticleCategory_Slug_key";

CREATE UNIQUE INDEX "UX_Tbl_ArticleCategory_Name_NotDeleted"
    ON "Tbl_ArticleCategory" ("Name")
    WHERE "IsDeleted" = false;

CREATE UNIQUE INDEX "UX_Tbl_ArticleCategory_Slug_NotDeleted"
    ON "Tbl_ArticleCategory" ("Slug")
    WHERE "IsDeleted" = false;

-- 4. Tbl_Article: drop inline UNIQUE constraint, create partial index
ALTER TABLE "Tbl_Article" DROP CONSTRAINT IF EXISTS "Tbl_Article_Slug_key";

CREATE UNIQUE INDEX "UX_Tbl_Article_Slug_NotDeleted"
    ON "Tbl_Article" ("Slug")
    WHERE "IsDeleted" = false;

-- 5. Tbl_Newsletter: drop inline UNIQUE constraint, create partial index
ALTER TABLE "Tbl_Newsletter" DROP CONSTRAINT IF EXISTS "Tbl_Newsletter_Email_key";

CREATE UNIQUE INDEX "UX_Tbl_Newsletter_Email_NotDeleted"
    ON "Tbl_Newsletter" ("Email")
    WHERE "IsDeleted" = false;

COMMIT;

-- ============================================================================
-- VERIFICATION: After running, verify the indexes exist:
-- SELECT indexname, indexdef FROM pg_indexes WHERE schemaname = 'public'
--   AND indexname LIKE 'UX_%_NotDeleted';
-- ============================================================================