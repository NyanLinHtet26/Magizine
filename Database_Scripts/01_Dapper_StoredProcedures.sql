-- ========================================================================
-- Magizine PostgreSQL Functions (Stored Procedures for Dapper)
-- ========================================================================

-- 1. ARTICLE CATEGORIES
DROP FUNCTION IF EXISTS fn_get_article_category_list;
CREATE OR REPLACE FUNCTION fn_get_article_category_list(
    p_search_keyword TEXT DEFAULT NULL,
    p_is_active BOOLEAN DEFAULT NULL,
    p_page INT DEFAULT 1,
    p_page_size INT DEFAULT 20
)
RETURNS TABLE (
    "TotalCount" BIGINT,
    "ArticleCategoryId" BIGINT,
    "Name" VARCHAR,
    "Slug" VARCHAR,
    "Description" VARCHAR,
    "SortOrder" INT,
    "CreatedAt" TIMESTAMP,
    "UpdatedAt" TIMESTAMP
) AS $$
DECLARE
    v_total BIGINT;
BEGIN
    SELECT COUNT(*) INTO v_total
    FROM "TblArticleCategories" c
    WHERE c."IsDeleted" = FALSE
      AND (p_search_keyword IS NULL OR c."Name" ILIKE '%' || p_search_keyword || '%')
      AND (p_is_active IS NULL OR c."IsActive" = p_is_active);

    RETURN QUERY
    SELECT 
        v_total,
        c."ArticleCategoryId",
        c."Name",
        c."Slug",
        c."Description",
        c."SortOrder",
        c."CreatedAt",
        c."UpdatedAt"
    FROM "TblArticleCategories" c
    WHERE c."IsDeleted" = FALSE
      AND (p_search_keyword IS NULL OR c."Name" ILIKE '%' || p_search_keyword || '%')
      AND (p_is_active IS NULL OR c."IsActive" = p_is_active)
    ORDER BY c."SortOrder" ASC
    OFFSET (p_page - 1) * p_page_size
    LIMIT p_page_size;
END;
$$ LANGUAGE plpgsql;

-- 2. AUTHORS
DROP FUNCTION IF EXISTS fn_get_author_list;
CREATE OR REPLACE FUNCTION fn_get_author_list(
    p_search_keyword TEXT DEFAULT NULL,
    p_is_approved BOOLEAN DEFAULT NULL,
    p_is_active BOOLEAN DEFAULT NULL,
    p_page INT DEFAULT 1,
    p_page_size INT DEFAULT 20
)
RETURNS TABLE (
    "TotalCount" BIGINT,
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
DECLARE
    v_total BIGINT;
BEGIN
    SELECT COUNT(*) INTO v_total
    FROM "TblAuthors" a
    WHERE a."IsDeleted" = FALSE
      AND (p_search_keyword IS NULL OR a."FirstName" ILIKE '%' || p_search_keyword || '%' OR a."LastName" ILIKE '%' || p_search_keyword || '%' OR a."Email" ILIKE '%' || p_search_keyword || '%')
      AND (p_is_approved IS NULL OR a."IsApproved" = p_is_approved)
      AND (p_is_active IS NULL OR a."IsActive" = p_is_active);

    RETURN QUERY
    SELECT 
        v_total,
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
    WHERE a."IsDeleted" = FALSE
      AND (p_search_keyword IS NULL OR a."FirstName" ILIKE '%' || p_search_keyword || '%' OR a."LastName" ILIKE '%' || p_search_keyword || '%' OR a."Email" ILIKE '%' || p_search_keyword || '%')
      AND (p_is_approved IS NULL OR a."IsApproved" = p_is_approved)
      AND (p_is_active IS NULL OR a."IsActive" = p_is_active)
    ORDER BY a."CreatedAt" DESC
    OFFSET (p_page - 1) * p_page_size
    LIMIT p_page_size;
END;
$$ LANGUAGE plpgsql;

-- 3. ARTICLES
DROP FUNCTION IF EXISTS fn_get_article_list;
CREATE OR REPLACE FUNCTION fn_get_article_list(
    p_search_keyword TEXT DEFAULT NULL,
    p_category_id BIGINT DEFAULT NULL,
    p_author_id BIGINT DEFAULT NULL,
    p_status VARCHAR DEFAULT NULL,
    p_is_featured BOOLEAN DEFAULT NULL,
    p_is_spotlight BOOLEAN DEFAULT NULL,
    p_page INT DEFAULT 1,
    p_page_size INT DEFAULT 20
)
RETURNS TABLE (
    "TotalCount" BIGINT,
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
DECLARE
    v_total BIGINT;
BEGIN
    SELECT COUNT(*) INTO v_total
    FROM "TblArticles" ar
    WHERE ar."IsDeleted" = FALSE
      AND (p_search_keyword IS NULL OR ar."Title" ILIKE '%' || p_search_keyword || '%' OR ar."SubTitle" ILIKE '%' || p_search_keyword || '%')
      AND (p_category_id IS NULL OR p_category_id <= 0 OR ar."ArticleCategoryId" = p_category_id)
      AND (p_author_id IS NULL OR p_author_id <= 0 OR ar."AuthorId" = p_author_id)
      AND (p_status IS NULL OR p_status = '' OR ar."Status" ILIKE p_status)
      AND (p_is_featured IS NULL OR ar."IsFeatured" = p_is_featured)
      AND (p_is_spotlight IS NULL OR ar."IsSpotlight" = p_is_spotlight);

    RETURN QUERY
    SELECT 
        v_total,
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
    WHERE ar."IsDeleted" = FALSE
      AND (p_search_keyword IS NULL OR ar."Title" ILIKE '%' || p_search_keyword || '%' OR ar."SubTitle" ILIKE '%' || p_search_keyword || '%')
      AND (p_category_id IS NULL OR p_category_id <= 0 OR ar."ArticleCategoryId" = p_category_id)
      AND (p_author_id IS NULL OR p_author_id <= 0 OR ar."AuthorId" = p_author_id)
      AND (p_status IS NULL OR p_status = '' OR ar."Status" ILIKE p_status)
      AND (p_is_featured IS NULL OR ar."IsFeatured" = p_is_featured)
      AND (p_is_spotlight IS NULL OR ar."IsSpotlight" = p_is_spotlight)
    ORDER BY COALESCE(ar."PublishedAt", ar."CreatedAt") DESC
    OFFSET (p_page - 1) * p_page_size
    LIMIT p_page_size;
END;
$$ LANGUAGE plpgsql;
