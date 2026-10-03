-- ========================================================================
-- Magizine PostgreSQL Functions for RequestArticle (Pitches)
-- ========================================================================

DROP FUNCTION IF EXISTS fn_get_request_article_list;
CREATE OR REPLACE FUNCTION fn_get_request_article_list(
    p_search_keyword TEXT DEFAULT NULL,
    p_status VARCHAR DEFAULT NULL,
    p_page INT DEFAULT 1,
    p_page_size INT DEFAULT 20
)
RETURNS TABLE (
    "TotalCount" BIGINT,
    "RequestArticleId" BIGINT,
    "PitcherName" VARCHAR,
    "PitcherEmail" VARCHAR,
    "ProposedTitle" VARCHAR,
    "PitchBody" TEXT,
    "ArticleCategoryId" BIGINT,
    "Status" VARCHAR,
    "SubmittedAt" TIMESTAMP,
    "ReviewedAt" TIMESTAMP,
    "ReviewedByAdminId" BIGINT,
    "AdminNotes" TEXT,
    "CreatedAt" TIMESTAMP,
    "UpdatedAt" TIMESTAMP
) AS $$
DECLARE
    v_total BIGINT;
BEGIN
    SELECT COUNT(*) INTO v_total
    FROM "TblRequestArticles" r
    WHERE r."IsDeleted" = FALSE
      AND (p_search_keyword IS NULL OR r."PitcherName" ILIKE '%' || p_search_keyword || '%' OR r."PitcherEmail" ILIKE '%' || p_search_keyword || '%')
      AND (p_status IS NULL OR p_status = '' OR r."Status" ILIKE p_status);

    RETURN QUERY
    SELECT 
        v_total,
        r."RequestArticleId",
        r."PitcherName",
        r."PitcherEmail",
        r."ProposedTitle",
        r."PitchBody",
        r."ArticleCategoryId",
        r."Status",
        r."SubmittedAt",
        r."ReviewedAt",
        r."ReviewedByAdminId",
        r."AdminNotes",
        r."CreatedAt",
        r."UpdatedAt"
    FROM "TblRequestArticles" r
    WHERE r."IsDeleted" = FALSE
      AND (p_search_keyword IS NULL OR r."PitcherName" ILIKE '%' || p_search_keyword || '%' OR r."PitcherEmail" ILIKE '%' || p_search_keyword || '%')
      AND (p_status IS NULL OR p_status = '' OR r."Status" ILIKE p_status)
    ORDER BY r."SubmittedAt" DESC
    OFFSET (p_page - 1) * p_page_size
    LIMIT p_page_size;
END;
$$ LANGUAGE plpgsql;


DROP FUNCTION IF EXISTS fn_get_request_article_by_id;
CREATE OR REPLACE FUNCTION fn_get_request_article_by_id(
    p_request_id BIGINT
)
RETURNS TABLE (
    "RequestArticleId" BIGINT,
    "PitcherName" VARCHAR,
    "PitcherEmail" VARCHAR,
    "ProposedTitle" VARCHAR,
    "PitchBody" TEXT,
    "ArticleCategoryId" BIGINT,
    "Status" VARCHAR,
    "SubmittedAt" TIMESTAMP,
    "ReviewedAt" TIMESTAMP,
    "ReviewedByAdminId" BIGINT,
    "AdminNotes" TEXT,
    "CreatedAt" TIMESTAMP,
    "UpdatedAt" TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        r."RequestArticleId",
        r."PitcherName",
        r."PitcherEmail",
        r."ProposedTitle",
        r."PitchBody",
        r."ArticleCategoryId",
        r."Status",
        r."SubmittedAt",
        r."ReviewedAt",
        r."ReviewedByAdminId",
        r."AdminNotes",
        r."CreatedAt",
        r."UpdatedAt"
    FROM "TblRequestArticles" r
    WHERE r."RequestArticleId" = p_request_id
      AND r."IsDeleted" = FALSE;
END;
$$ LANGUAGE plpgsql;
