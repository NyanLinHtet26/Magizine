#!/usr/bin/env bash
# ==============================================================================
# L'ÉDITION — MagizineBackend Project Structure & Architecture Tool
# ==============================================================================
# Visualizes, audits, and outputs the structural architecture, endpoints catalog,
# and database schema of the MagizineBackend solution.
# ==============================================================================

set -eo pipefail

CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
MAGENTA='\033[0;35m'
RED='\033[0;31m'
BOLD='\033[1m'
NC='\033[0m' # No Color

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

print_header() {
    echo -e "${CYAN}${BOLD}"
    echo "======================================================================"
    echo "       L'ÉDITION (MagizineBackend) — Project Structure Inspector      "
    echo "======================================================================"
    echo -e "${NC}"
}

cmd_summary() {
    print_header
    echo -e "${BOLD}Solution:${NC} MagizineBackend.slnx (.NET 10.0 / C# 13)"
    echo -e "${BOLD}Architecture Pattern:${NC} Vertical-Slice Feature Architecture + CQRS Read/Write Split"
    echo -e "${BOLD}Identity Standard:${NC} Universal ULID (VARCHAR(26) Crockford Base32)"
    echo ""
    echo -e "${BOLD}Core Solution Layers:${NC}"
    echo -e "  1. ${GREEN}MagizinePublic.Api${NC}  → Public reader portal (Port 5080/7080)"
    echo -e "  2. ${GREEN}MagizineAuthor.Api${NC}  → Contributing author workspace (Port 5267/7137)"
    echo -e "  3. ${GREEN}MagizineAdmin.Api${NC}   → Editorial back-office governance (Port 5024/7036)"
    echo -e "  4. ${GREEN}Magizine.DataBase${NC}   → EF Core 10 Command layer with soft-delete filters"
    echo -e "  5. ${GREEN}Magizine.Shared${NC}     → Domain contracts, DapperService, JWT & Result<T>"
    echo -e "  6. ${GREEN}Magizine.Tests${NC}      → Unit & Integration test suite"
    echo ""
}

cmd_tree() {
    print_header
    echo -e "${YELLOW}${BOLD}Physical Repository Structure:${NC}"
    cat << 'EOF'
MagizineBackend/
├── MagizineBackend.slnx                    [Modern XML Solution File]
├── PROJECT_DOCUMENTATION.md               [Master Architecture & Specs Document]
├── README.md                              [Quick-Start Guide]
├── skill.sh                               [CLI Automation & Developer Operations]
├── structure.sh                           [Structure Inspector & Audit Tool]
│
├── MagizinePublic.Api/                     [Reader Gateway - Port 5080]
│   ├── Features/
│   │   ├── Article/                       - Public published articles & slug retrieval
│   │   ├── ArticleCategory/               - Public categories feed
│   │   └── RequestArticle/                - Unsolicited article pitch submissions
│   ├── FeatureManager.cs                  - Modular service registration root
│   ├── GlobalExceptionHandler.cs          - Centralized ErrorEnvelope mapper
│   └── Program.cs                         - Minimal HTTP pipeline & Scalar UI
│
├── MagizineAuthor.Api/                     [Author Workspace - Port 5267]
│   ├── Features/
│   │   ├── Article/                       - Author draft creation, editing & review
│   │   └── ArticleCategory/               - Category list for draft composition
│   ├── FeatureManager.cs                  - Modular registration with JWT auth
│   └── Program.cs                         - Pipeline with authentication
│
├── MagizineAdmin.Api/                      [Editorial Back-Office - Port 5024]
│   ├── Features/
│   │   ├── Article/                       - Editorial publishing curation & status change
│   │   ├── ArticleCategory/               - Full category management & slug generation
│   │   ├── Author/                        - Author onboarding & status controls (Ban/Suspend)
│   │   └── RequestArticle/                - Pitch review workflow (Approve/Reject)
│   ├── FeatureManager.cs                  - Composition root with strict JWT & CORS
│   └── Program.cs                         - Admin pipeline with OpenAPI & Scalar
│
├── Magizine.DataBase/                      [Command & Persistence Layer (EF Core 10)]
│   ├── EFContext/
│   │   ├── AppDbContext.cs                - Fluent API model configurations
│   │   └── Tbl*.cs (10 entities)          - ULID entity models (VARCHAR(26) PK & FKs)
│   ├── Paging/
│   │   └── QueryablePagingExtensions.cs   - EF Core LINQ pagination extensions
│   ├── DependencyInjection.cs             - AddMagizineDatabase() configuration wire
│   └── MagizineDbContext.cs               - Derived DbContext with soft-delete filters
│
├── Magizine.Shared/                        [Domain Core, DTOs & CQRS Read Engine]
│   ├── Enums/                             - Domain enums (Roles, Status, RespType)
│   ├── Exceptions/                        - ErrorEnvelope & validation builders
│   ├── JsonResources/                     - Centralized response codes
│   ├── Models/                            - Request & Response DTOs + Result<T> monad
│   ├── Security/                          - JWT generator, PasswordHasher, CORS options
│   └── Services/
│       └── DapperService.cs               - High-performance stored procedure engine
│
├── Magizine.Tests/                         [Test Suite (xUnit + Moq + FluentAssertions)]
│   ├── Enums/                             - Enum serialization & mapping tests
│   ├── Exceptions/                        - Error mapping & envelope tests
│   ├── Features/ArticleCategory/          - Unit & integration tests
│   ├── Paging/                            - Pagination calculation tests
│   └── Security/                          - JWT validation, PBKDF2 hashing, CORS tests
│
├── Database_Scripts/                       [Database DDL & Stored Procedures]
│   ├── Full_Database_Script_SqlServer.sql - Complete SQL Server T-SQL script
│   └── Full_Database_Script.sql           - PostgreSQL / Supabase schema & functions
│
└── Doc/                                    [Documentation & Rescaffolding Scripts]
    ├── Rescaffold.ps1                     - Safe EF Core reverse engineering automation
    └── *.xlsx                             - Architecture & schema design sheets
EOF
    echo ""
}

cmd_endpoints() {
    print_header
    echo -e "${YELLOW}${BOLD}Catalog of Endpoints by API Gateway:${NC}"
    echo ""
    echo -e "${CYAN}=== 1. MagizinePublic.Api (Port 5080) ===${NC}"
    echo "  GET   /api/public/article/list             -> Published article feed"
    echo "  GET   /api/public/article/{slug}           -> Read article by slug"
    echo "  GET   /api/public/article-category/list    -> Active categories"
    echo "  GET   /api/public/article-category/{slug}  -> Category by slug"
    echo "  POST  /api/public/requestarticle/submit    -> Submit unsolicited pitch"
    echo ""
    echo -e "${CYAN}=== 2. MagizineAuthor.Api (Port 5267) ===${NC}"
    echo "  POST  /api/author/article/List             -> My articles (strict author isolation)"
    echo "  POST  /api/author/article/Detail           -> Get article detail"
    echo "  POST  /api/author/article/Create           -> Create draft (auto ULID + slug)"
    echo "  POST  /api/author/article/Update           -> Update existing draft"
    echo "  POST  /api/author/article/SubmitForReview  -> Submit draft for editorial review"
    echo "  GET   /api/author/article-category/list    -> Category dropdown for drafting"
    echo ""
    echo -e "${CYAN}=== 3. MagizineAdmin.Api (Port 5024) ===${NC}"
    echo "  POST  /api/admin/article/List              -> All articles overview"
    echo "  POST  /api/admin/article/Detail            -> Article detail with audit data"
    echo "  POST  /api/admin/article/Create            -> Create article directly"
    echo "  POST  /api/admin/article/Update            -> Update article content/metadata"
    echo "  POST  /api/admin/article/ChangeStatus      -> Publish, Feature, Spotlight, Reject"
    echo "  POST  /api/admin/article/Delete            -> Soft-delete article"
    echo "  POST  /api/admin/article-category/List     -> Paginated categories"
    echo "  POST  /api/admin/article-category/Detail   -> Category detail"
    echo "  POST  /api/admin/article-category/Create   -> Create category"
    echo "  POST  /api/admin/article-category/Update   -> Update category"
    echo "  POST  /api/admin/article-category/Delete   -> Soft-delete category"
    echo "  POST  /api/admin/author/List               -> Paginated authors"
    echo "  POST  /api/admin/author/Detail             -> Author profile"
    echo "  POST  /api/admin/author/Create             -> Onboard author"
    echo "  POST  /api/admin/author/Update             -> Update author profile"
    echo "  POST  /api/admin/author/ChangeStatus       -> Suspend, Ban, Activate author"
    echo "  POST  /api/admin/author/Delete             -> Soft-delete author"
    echo "  POST  /api/admin/requestarticle/List       -> Pitches queue"
    echo "  POST  /api/admin/requestarticle/Detail     -> Pitch detail"
    echo "  POST  /api/admin/requestarticle/Review     -> Approve / Reject pitch"
    echo ""
}

cmd_schema() {
    print_header
    echo -e "${YELLOW}${BOLD}Database Schema & Stored Procedures:${NC}"
    echo ""
    echo -e "${BOLD}10 Tables (ULID PK & FKs: VARCHAR(26)):${NC}"
    echo "  1.  Tbl_Admin            [Soft Delete: Yes] - Admins & credentials"
    echo "  2.  Tbl_Author           [Soft Delete: Yes] - Authors & biographical info"
    echo "  3.  Tbl_ArticleCategory  [Soft Delete: Yes] - Categories & sort orders"
    echo "  4.  Tbl_Article          [Soft Delete: Yes] - Articles, slugs, status, markdown body"
    echo "  5.  Tbl_RequestArticle   [Soft Delete: Yes] - Unsolicited article pitches"
    echo "  6.  Tbl_ContactMessage   [Soft Delete: Yes] - Reader inquiries & replies"
    echo "  7.  Tbl_Newsletter       [Soft Delete: Yes] - Newsletter email subscribers"
    echo "  8.  Tbl_AboutAndAppData  [Soft Delete: No ] - Site metadata & configurations"
    echo "  9.  Tbl_Ads              [Soft Delete: Yes] - Advertisement banners & clicks"
    echo "  10. Tbl_Notification     [Soft Delete: Yes] - In-app author & reader notifications"
    echo ""
    echo -e "${BOLD}10 High-Performance Stored Procedures (Executed by DapperService):${NC}"
    echo "  1.  fn_get_article_list            (Search, CategoryId, AuthorId, Status, Featured, Spotlight, Page, PageSize)"
    echo "  2.  fn_get_article_by_id          (ArticleId, Optional AuthorId)"
    echo "  3.  fn_get_article_by_slug        (Slug)"
    echo "  4.  fn_get_article_category_list  (Search, IsActive, Page, PageSize)"
    echo "  5.  fn_get_article_category_by_id (CategoryId)"
    echo "  6.  fn_get_article_category_by_slug (Slug)"
    echo "  7.  fn_get_author_list            (Search, Status, Page, PageSize)"
    echo "  8.  fn_get_author_by_id           (AuthorId)"
    echo "  9.  fn_get_request_article_list   (Search, CategoryId, Status, Page, PageSize)"
    echo "  10. fn_get_request_article_by_id  (RequestArticleId)"
    echo ""
}

cmd_validate() {
    print_header
    echo -e "${YELLOW}${BOLD}Validating Project Structure Integrity...${NC}"
    echo ""

    local FAILED=0

    check_item() {
        local DESC="$1"
        local PATH="$2"
        if [ -e "$PATH" ]; then
            echo -e "  [${GREEN}OK${NC}] $DESC: ${PATH}"
        else
            echo -e "  [${RED}MISSING${NC}] $DESC: ${PATH}"
            FAILED=$((FAILED + 1))
        fi
    }

    echo -e "${BOLD}1. Solution & Projects:${NC}"
    check_item "Solution file" "MagizineBackend.slnx"
    check_item "Public API" "MagizinePublic.Api/MagizinePublic.Api.csproj"
    check_item "Author API" "MagizineAuthor.Api/MagizineAuthor.Api.csproj"
    check_item "Admin API" "MagizineAdmin.Api/MagizineAdmin.Api.csproj"
    check_item "Database project" "Magizine.DataBase/Magizine.DataBase.csproj"
    check_item "Shared project" "Magizine.Shared/Magizine.Shared.csproj"
    check_item "Test project" "Magizine.Tests/Magizine.Tests.csproj"
    echo ""

    echo -e "${BOLD}2. Core Scripts & Documentation:${NC}"
    check_item "Documentation" "PROJECT_DOCUMENTATION.md"
    check_item "Readme" "README.md"
    check_item "Developer CLI" "skill.sh"
    check_item "SQL Server Master Script" "Database_Scripts/Full_Database_Script_SqlServer.sql"
    check_item "PostgreSQL Master Script" "Database_Scripts/Full_Database_Script.sql"
    echo ""

    echo -e "${BOLD}3. Key Source Files:${NC}"
    check_item "EF Context" "Magizine.DataBase/MagizineDbContext.cs"
    check_item "Dapper Service" "Magizine.Shared/Services/DapperService.cs"
    check_item "Result Monad" "Magizine.Shared/Models/Result.cs"
    check_item "JWT Generator" "Magizine.Shared/Security/JwtTokenGenerator.cs"
    echo ""

    if [ "$FAILED" -eq 0 ]; then
        echo -e "${GREEN}${BOLD}✔ All structural components verified successfully! No missing files.${NC}"
    else
        echo -e "${RED}${BOLD}✘ Found $FAILED missing structural components.${NC}"
        exit 1
    fi
}

cmd_help() {
    print_header
    echo -e "${BOLD}Usage:${NC} ./structure.sh <command>"
    echo ""
    echo -e "${BOLD}Available Commands:${NC}"
    echo -e "  ${GREEN}tree${NC}        Display visual directory hierarchy of the solution"
    echo -e "  ${GREEN}summary${NC}     Show high-level architectural overview & ports"
    echo -e "  ${GREEN}endpoints${NC}   List all API gateways, routes, and actions"
    echo -e "  ${GREEN}schema${NC}      Display 10 database tables and stored procedures"
    echo -e "  ${GREEN}validate${NC}    Validate all projects, files, and scripts exist"
    echo -e "  ${GREEN}help${NC}        Show this help message"
    echo ""
    echo -e "${BOLD}Examples:${NC}"
    echo "  ./structure.sh tree"
    echo "  ./structure.sh endpoints"
    echo "  ./structure.sh validate"
    echo ""
}

COMMAND="${1:-summary}"

case "$COMMAND" in
    tree)
        cmd_tree
        ;;
    summary)
        cmd_summary
        ;;
    endpoints)
        cmd_endpoints
        ;;
    schema|db)
        cmd_schema
        ;;
    validate|check)
        cmd_validate
        ;;
    help|--help|-h)
        cmd_help
        ;;
    all)
        cmd_summary
        cmd_tree
        cmd_endpoints
        cmd_schema
        cmd_validate
        ;;
    *)
        echo -e "${RED}Unknown command: $COMMAND${NC}"
        echo "Run './structure.sh help' for usage instructions."
        exit 1
        ;;
esac
