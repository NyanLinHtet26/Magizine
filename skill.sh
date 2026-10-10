#!/usr/bin/env bash
# ==============================================================================
# L'ÉDITION — MagizineBackend CLI Management Skill Script
# ==============================================================================
# This script automates common developer tasks, builds, testing, database scripts
# verification, and running the multiple APIs across Linux, macOS, WSL, & Git Bash.
# ==============================================================================

set -eo pipefail

# ANSI Color Codes
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BOLD='\033[1m'
NC='\033[0m' # No Color

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

print_header() {
    echo -e "${CYAN}${BOLD}"
    echo "======================================================================"
    echo "       L'ÉDITION (MagizineBackend) — Developer Skill CLI"
    echo "======================================================================"
    echo -e "${NC}"
}

print_help() {
    print_header
    echo -e "${BOLD}Usage:${NC} ./skill.sh <command> [options]"
    echo ""
    echo -e "${BOLD}Available Commands:${NC}"
    echo -e "  ${GREEN}build${NC}         Build all projects in the solution"
    echo -e "  ${GREEN}clean${NC}         Clean all bin/ and obj/ directories"
    echo -e "  ${GREEN}restore${NC}       Restore NuGet packages (with offline cache fallback)"
    echo -e "  ${GREEN}test${NC}          Run the test suite with host timeout config"
    echo -e "  ${GREEN}format${NC}        Verify and format C# code style using dotnet-format"
    echo -e "  ${GREEN}check-ulid${NC}    Scan the codebase for legacy long/BIGINT ID references"
    echo -e "  ${GREEN}run-admin${NC}     Start MagizineAdmin.Api (Port 5024)"
    echo -e "  ${GREEN}run-author${NC}    Start MagizineAuthor.Api (Port 5267)"
    echo -e "  ${GREEN}run-public${NC}    Start MagizinePublic.Api (Port 5080)"
    echo -e "  ${GREEN}db-info${NC}       Print database schema and stored procedures info"
    echo -e "  ${GREEN}structure${NC}     Inspect and validate project architecture and file tree"
    echo -e "  ${GREEN}scaffold${NC}      Regenerate EF Core models from Microsoft SQL Server"
    echo -e "  ${GREEN}help${NC}          Show this help message"
    echo ""
    echo -e "${BOLD}Examples:${NC}"
    echo "  ./skill.sh build"
    echo "  ./skill.sh run-admin"
    echo "  ./skill.sh check-ulid"
    echo ""
}

cmd_build() {
    print_header
    echo -e "${YELLOW}Building solution MagizineBackend.slnx...${NC}"
    dotnet build MagizineBackend.slnx
    echo -e "${GREEN}Build completed successfully!${NC}"
}

cmd_clean() {
    print_header
    echo -e "${YELLOW}Cleaning build artifacts (bin/ and obj/)...${NC}"
    dotnet clean MagizineBackend.slnx
    find . -type d \( -name "bin" -o -name "obj" \) -not -path "*/.*" -prune -exec rm -rf {} + 2>/dev/null || true
    echo -e "${GREEN}Clean completed!${NC}"
}

cmd_restore() {
    print_header
    echo -e "${YELLOW}Restoring NuGet packages...${NC}"
    if [ -d "$HOME/.nuget/packages" ]; then
        echo -e "${CYAN}Using offline cache fallback: $HOME/.nuget/packages${NC}"
        dotnet restore MagizineBackend.slnx --source "$HOME/.nuget/packages" || dotnet restore MagizineBackend.slnx
    else
        dotnet restore MagizineBackend.slnx
    fi
    echo -e "${GREEN}Packages restored successfully!${NC}"
}

cmd_test() {
    print_header
    echo -e "${YELLOW}Running tests in Magizine.Tests...${NC}"
    export VSTEST_CONNECTION_TIMEOUT=300
    export DOTNET_CLI_VSTEST_HOST_IP="127.0.0.1"
    dotnet test Magizine.Tests/Magizine.Tests.csproj --no-restore "$@"
}

cmd_format() {
    print_header
    echo -e "${YELLOW}Formatting code in solution...${NC}"
    dotnet format MagizineBackend.slnx
    echo -e "${GREEN}Code formatted according to conventions!${NC}"
}

cmd_check_ulid() {
    print_header
    echo -e "${YELLOW}Scanning codebase for legacy long/BIGINT ID references...${NC}"
    
    local VIOLATIONS
    VIOLATIONS=$(grep -rnE "(long\??\s+[A-Za-z0-9]*Id\b)" \
        --exclude-dir={obj,bin,.git} \
        --include="*.cs" . || true)

    if [ -n "$VIOLATIONS" ]; then
        echo -e "${RED}${BOLD}Found potential long ID occurrences:${NC}"
        echo "$VIOLATIONS"
    else
        echo -e "${GREEN}No legacy long ID declarations found! Universal ULID adhered to.${NC}"
    fi
}

cmd_run_admin() {
    print_header
    echo -e "${CYAN}Starting MagizineAdmin.Api on http://localhost:5024 (HTTPS: 7036)...${NC}"
    echo -e "${YELLOW}Interactive Scalar API docs: http://localhost:5024/scalar/v1${NC}"
    dotnet run --project MagizineAdmin.Api/MagizineAdmin.Api.csproj --launch-profile http
}

cmd_run_author() {
    print_header
    echo -e "${CYAN}Starting MagizineAuthor.Api on http://localhost:5267 (HTTPS: 7137)...${NC}"
    echo -e "${YELLOW}Interactive Scalar API docs: http://localhost:5267/scalar/v1${NC}"
    dotnet run --project MagizineAuthor.Api/MagizineAuthor.Api.csproj --launch-profile http
}

cmd_run_public() {
    print_header
    echo -e "${CYAN}Starting MagizinePublic.Api on http://localhost:5080 (HTTPS: 7080)...${NC}"
    echo -e "${YELLOW}Interactive Scalar API docs: http://localhost:5080/scalar/v1${NC}"
    dotnet run --project MagizinePublic.Api/MagizinePublic.Api.csproj --launch-profile http
}

cmd_db_info() {
    print_header
    echo -e "${BOLD}Database Scripts Summary:${NC}"
    echo -e "  - SQL Server T-SQL Script: ${CYAN}Database_Scripts/Full_Database_Script_SqlServer.sql${NC}"
    echo -e "  - PostgreSQL / Supabase:   ${CYAN}Database_Scripts/Full_Database_Script.sql${NC}"
    echo ""
    echo -e "${BOLD}10 Tables with ULID Primary Keys (VARCHAR(26)):${NC}"
    echo "  1.  Tbl_Admin           (AdminId)"
    echo "  2.  Tbl_Author          (AuthorId)"
    echo "  3.  Tbl_ArticleCategory (ArticleCategoryId)"
    echo "  4.  Tbl_Article         (ArticleId, ArticleCategoryId, AuthorId)"
    echo "  5.  Tbl_RequestArticle  (RequestArticleId, ArticleCategoryId, ProcessedByAdminId)"
    echo "  6.  Tbl_ContactMessage  (ContactMessageId)"
    echo "  7.  Tbl_Newsletter      (NewsletterId)"
    echo "  8.  Tbl_AboutAndAppData (AboutAndAppId)"
    echo "  9.  Tbl_Ads             (AdId)"
    echo "  10. Tbl_Notification    (NotificationId, UserId, ArticleId)"
    echo ""
    echo -e "${BOLD}10 High-Performance Stored Procedures (Dapper Queries):${NC}"
    echo "  1.  fn_get_article_list            (search, category, author, status, featured, spotlight)"
    echo "  2.  fn_get_article_by_id          (article_id, optional author_id)"
    echo "  3.  fn_get_article_by_slug        (slug)"
    echo "  4.  fn_get_article_category_list  (search, is_active)"
    echo "  5.  fn_get_article_category_by_id (category_id)"
    echo "  6.  fn_get_article_category_by_slug (slug)"
    echo "  7.  fn_get_author_list            (search, status)"
    echo "  8.  fn_get_author_by_id           (author_id)"
    echo "  9.  fn_get_request_article_list   (search, category_id, status)"
    echo "  10. fn_get_request_article_by_id  (request_article_id)"
    echo ""
}

# Main Command Dispatcher
COMMAND="${1:-help}"

case "$COMMAND" in
    build)
        cmd_build
        ;;
    clean)
        cmd_clean
        ;;
    restore)
        cmd_restore
        ;;
    test)
        shift
        cmd_test "$@"
        ;;
    format)
        cmd_format
        ;;
    check-ulid)
        cmd_check_ulid
        ;;
    run-admin)
        cmd_run_admin
        ;;
    run-author)
        cmd_run_author
        ;;
    run-public)
        cmd_run_public
        ;;
    db-info)
        cmd_db_info
        ;;
    structure|tree)
        shift
        bash ./structure.sh "${@:-summary}"
        ;;
    scaffold)
        powershell -ExecutionPolicy Bypass -File ./Doc/Rescaffold.ps1
        ;;
    help|--help|-h)
        print_help
        ;;
    *)
        echo -e "${RED}Unknown command: $COMMAND${NC}"
        echo "Run './skill.sh help' to see available commands."
        exit 1
        ;;
esac
