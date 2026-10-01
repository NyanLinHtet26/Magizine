-- ========================================================================
-- Magizine PostgreSQL Functions (Detail/Single Item Stored Procedures)
-- ========================================================================

-- 1. ARTICLE CATEGORIES (By ID)
DROP FUNCTION IF EXISTS fn_get_article_category_by_id;
CREATE OR REPLACE FUNCTION fn_get_article_category_by_id(
    p_category_id BIGINT
)
RETURNS TABLE (
    "ArticleCategoryId" BIGINT,
    "Name" VARCHAR,
    "Slug" VARCHAR,
    "Description" VARCHAR,
    "IsActive" BOOLEAN,
    "SortOrder" INT,
    "CreatedAt" TIMESTAMP,
    "UpdatedAt" TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c."ArticleCategoryId",
        c."Name",
        c."Slug",
        c."Description",
        c."IsActive",
        c."SortOrder",
        c."CreatedAt",
        c."UpdatedAt"
    FROM "TblArticleCategories" c
    WHERE c."ArticleCategoryId" = p_category_id
      AND c."IsDeleted" = FALSE;
END;
$$ LANGUAGE plpgsql;


-- 1.b ARTICLE CATEGORIES (By SLUG)
DROP FUNCTION IF EXISTS fn_get_article_category_by_slug;
CREATE OR REPLACE FUNCTION fn_get_article_category_by_slug(
    p_slug VARCHAR
)
RETURNS TABLE (
    "ArticleCategoryId" BIGINT,
    "Name" VARCHAR,
    "Slug" VARCHAR,
    "Description" VARCHAR,
    "IsActive" BOOLEAN,
    "SortOrder" INT,
    "CreatedAt" TIMESTAMP,
    "UpdatedAt" TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c."ArticleCategoryId",
        c."Name",
        c."Slug",
        c."Description",
        c."IsActive",
        c."SortOrder",
        c."CreatedAt",
        c."UpdatedAt"
    FROM "TblArticleCategories" c
    WHERE c."Slug" = p_slug
      AND c."IsDeleted" = FALSE;
END;
$$ LANGUAGE plpgsql;


-- 2. AUTHORS (By ID)
DROP FUNCTION IF EXISTS fn_get_author_by_id;
CREATE OR REPLACE FUNCTION fn_get_author_by_id(
    p_author_id BIGINT
)
RETURNS TABLE (
    "AuthorId" BIGINT,
    "FirstName" VARCHAR,
    "LastName" VARCHAR,
    "Email" VARCHAR,
    "Title" VARCHAR,
    "Bio" TEXT,
    "PhotoUrl" VARCHAR,
    "InstagramUrl" VARCHAR,
    "TwitterUrl" VARCHAR,
    "WebsiteUrl" VARCHAR,
    "Slug" VARCHAR,
    "IsApproved" BOOLEAN,
    "IsActive" BOOLEAN,
    "CreatedAt" TIMESTAMP,
    "UpdatedAt" TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        a."AuthorId",
        a."FirstName",
        a."LastName",
        a."Email",
        a."Title",
        a."Bio",
        a."PhotoUrl",
        a."InstagramUrl",
        a."TwitterUrl",
        a."WebsiteUrl",
        a."Slug",
        a."IsApproved",
        a."IsActive",
        a."CreatedAt",
        a."UpdatedAt"
    FROM "TblAuthors" a
    WHERE a."AuthorId" = p_author_id
      AND a."IsDeleted" = FALSE;
END;
$$ LANGUAGE plpgsql;


-- 3. ARTICLES (By ID)
DROP FUNCTION IF EXISTS fn_get_article_by_id;
CREATE OR REPLACE FUNCTION fn_get_article_by_id(
    p_article_id BIGINT,
    p_author_id BIGINT DEFAULT NULL -- used for Author Api to lock to their own article
)
RETURNS TABLE (
    "ArticleId" BIGINT,
    "Title" VARCHAR,
    "SubTitle" VARCHAR,
    "Slug" VARCHAR,
    "Body" TEXT,
    "PhotoUrl" VARCHAR,
    "PhotoCaption" VARCHAR,
    "PhotoCredit" VARCHAR,
    "ArticleCategoryId" BIGINT,
    "CategoryName" VARCHAR,
    "AuthorId" BIGINT,
    "AuthorName" VARCHAR,
    "Status" VARCHAR,
    "PublishedAt" TIMESTAMP,
    "IsFeatured" BOOLEAN,
    "IsSpotlight" BOOLEAN,
    "CreatedAt" TIMESTAMP,
    "UpdatedAt" TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        ar."ArticleId",
        ar."Title",
        ar."SubTitle",
        ar."Slug",
        ar."Body",
        ar."PhotoUrl",
        ar."PhotoCaption",
        ar."PhotoCredit",
        ar."ArticleCategoryId",
        c."Name" AS "CategoryName",
        ar."AuthorId",
        (a."FirstName" || ' ' || a."LastName")::VARCHAR AS "AuthorName",
        ar."Status",
        ar."PublishedAt",
        ar."IsFeatured",
        ar."IsSpotlight",
        ar."CreatedAt",
        ar."UpdatedAt"
    FROM "TblArticles" ar
    INNER JOIN "TblArticleCategories" c ON ar."ArticleCategoryId" = c."ArticleCategoryId"
    INNER JOIN "TblAuthors" a ON ar."AuthorId" = a."AuthorId"
    WHERE ar."ArticleId" = p_article_id
      AND ar."IsDeleted" = FALSE
      AND (p_author_id IS NULL OR ar."AuthorId" = p_author_id);
END;
$$ LANGUAGE plpgsql;


-- 3.b ARTICLES (By SLUG)
DROP FUNCTION IF EXISTS fn_get_article_by_slug;
CREATE OR REPLACE FUNCTION fn_get_article_by_slug(
    p_slug VARCHAR
)
RETURNS TABLE (
    "ArticleId" BIGINT,
    "Title" VARCHAR,
    "SubTitle" VARCHAR,
    "Slug" VARCHAR,
    "Body" TEXT,
    "PhotoUrl" VARCHAR,
    "PhotoCaption" VARCHAR,
    "PhotoCredit" VARCHAR,
    "ArticleCategoryId" BIGINT,
    "CategoryName" VARCHAR,
    "AuthorId" BIGINT,
    "AuthorName" VARCHAR,
    "Status" VARCHAR,
    "PublishedAt" TIMESTAMP,
    "IsFeatured" BOOLEAN,
    "IsSpotlight" BOOLEAN,
    "CreatedAt" TIMESTAMP,
    "UpdatedAt" TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        ar."ArticleId",
        ar."Title",
        ar."SubTitle",
        ar."Slug",
        ar."Body",
        ar."PhotoUrl",
        ar."PhotoCaption",
        ar."PhotoCredit",
        ar."ArticleCategoryId",
        c."Name" AS "CategoryName",
        ar."AuthorId",
        (a."FirstName" || ' ' || a."LastName")::VARCHAR AS "AuthorName",
        ar."Status",
        ar."PublishedAt",
        ar."IsFeatured",
        ar."IsSpotlight",
        ar."CreatedAt",
        ar."UpdatedAt"
    FROM "TblArticles" ar
    INNER JOIN "TblArticleCategories" c ON ar."ArticleCategoryId" = c."ArticleCategoryId"
    INNER JOIN "TblAuthors" a ON ar."AuthorId" = a."AuthorId"
    WHERE ar."Slug" = p_slug
      AND ar."IsDeleted" = FALSE;
END;
$$ LANGUAGE plpgsql;
