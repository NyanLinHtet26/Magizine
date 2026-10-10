# L'ÉDITION — Digital Magazine Platform (MagizineBackend)
## Comprehensive Technical Documentation: Scope, Summary, Architecture, Usage, Structure & Code Style

---

## 1. Project Scope

### 1.1 Overview & Vision
**L'ÉDITION** is an enterprise-grade digital magazine and journalistic publishing platform designed to power modern online publications. It delivers high-throughput content delivery for readers, a streamlined editorial workflow for staff authors and freelance contributors, and a governance back-office for publication administrators.

### 1.2 Target Personas & API Boundaries
The backend is architected as three dedicated API gateways sharing unified data and core domain packages:

1. **Public Readers (`MagizinePublic.Api`)**
   - Read published articles with full pagination, search, category filtering, spotlight, and featured curation.
   - Access articles and categories by SEO-friendly slugs.
   - Unauthenticated freelance pitch submission (`RequestArticle`): freelance writers can submit pitches directly to the editorial team.
   - High-throughput, read-heavy, low-latency traffic.

2. **Contributing Authors & Journalists (`MagizineAuthor.Api`)**
   - Author workspace to create and manage personal drafts.
   - Automatic slug generation with collision resistance.
   - Submit drafted or rejected articles for editorial review (`Pending` status).
   - Strict author isolation: authors can only view, edit, or submit articles where `AuthorId` matches their authenticated identity.

3. **Editorial & Platform Administrators (`MagizineAdmin.Api`)**
   - Full publication governance, article review, publishing lifecycle (`Draft`, `Pending`, `Reviewing`, `Published`, `Rejected`, `Archived`).
   - Category management (create, update, sort order, activate/deactivate, soft delete).
   - Author profile management and status controls (`Active`, `Suspended`, `Banned`).
   - Pitch review workflow: approve pitches or reject them with feedback notes.
   - Audit logging: all deletions and reviews record `DeletedByAdminId` and `ProcessedByAdminId`.

### 1.3 Out-of-Scope / Future Iterations
- Native newsletter mail-blaster daemon (entity and opt-in ready).
- Ad campaign impression tracker engine (entity and placement models ready).
- Real-time WebSockets / Push Notifications (notification entity and relations modeled).
- Direct reader comments and interaction threads.

---

## 2. Executive Summary

| Attribute | Specification |
|---|---|
| **Runtime & SDK** | .NET 10.0 (`net10.0`), C# 13 |
| **Architectural Style** | Modular Vertical-Slice Feature Architecture with CQRS Read/Write split |
| **Command ORM** | Entity Framework Core 10 (`Magizine.DataBase`) with global soft-delete query filters |
| **Query Engine** | Dapper 2.1 via optimized T-SQL / PostgreSQL Stored Procedures (`DapperService`) |
| **Identity Standard** | Universally Unique Lexicographically Sortable Identifier (**ULID** - 26 chars Crockford Base32) |
| **Supported Databases** | Microsoft SQL Server (T-SQL scripts included) & PostgreSQL / Supabase |
| **Authentication** | RFC 7519 JWT Bearer tokens, HMAC-SHA256, 32+ byte key verification |
| **Serialization Standard** | PascalCase wire JSON contract with string-serialized enums |
| **Result Monad** | `Result<T>` with standardized `ErrorEnvelope` across all endpoints |
| **API Documentation** | Scalar Interactive API Explorer + OpenAPI 3.1 |

---

## 3. Architecture

### 3.1 Solution Architecture Diagram

```
                                  +-----------------------------+
                                  |       Client Applications   |
                                  | (Web, Admin Portal, Mobile) |
                                  +--------------+--------------+
                                                 |
                       +-------------------------+-------------------------+
                       | HTTP / JSON (PascalCase)|                         |
                       v                         v                         v
        +-----------------------------+ +--------------------+ +--------------------+
        |     MagizinePublic.Api      | | MagizineAuthor.Api | | MagizineAdmin.Api  |
        |      (Port 5080/7080)       | |  (Port 5267/7137)  | |  (Port 5024/7036)  |
        +--------------+--------------+ +---------+----------+ +---------+----------+
                       |                          |                      |
                       +--------------------+     |     +----------------+
                                            |     |     |
                                            v     v     v
                                  +-----------------------------+
                                  |       Magizine.Shared       |
                                  |  - Result<T> Monad          |
                                  |  - DapperService            |
                                  |  - JwtTokenGenerator & Auth |
                                  |  - DTO Models & Enums       |
                                  |  - ErrorMapper & Exceptions |
                                  +--------------+--------------+
                                                 |
                                                 v
                                  +-----------------------------+
                                  |      Magizine.DataBase      |
                                  |  - MagizineDbContext        |
                                  |  - 10 ULID Entity Models    |
                                  |  - Global Soft Delete Filter|
                                  |  - Queryable Paging Ext     |
                                  +--------------+--------------+
                                                 |
                       +-------------------------+-------------------------+
                       |                                                   |
              (Commands & Writes: EF Core)                        (Fast Reads: Dapper)
                       |                                                   |
                       v                                                   v
        +---------------------------------------------------------------------------+
        |                               DATABASE LAYER                              |
        | - SQL Server (Full_Database_Script_SqlServer.sql) OR PostgreSQL (Supabase)|
        | - 10 Tables: ULID Primary Keys (VARCHAR(26)) & Foreign Keys               |
        | - 10 High-Performance Stored Procedures / Functions                       |
        +---------------------------------------------------------------------------+
```

### 3.2 CQRS Read/Write Split Pattern
The architecture strictly decouples commands and queries to maximize performance and maintainability:
- **Write Path (Commands)**: Handled via Entity Framework Core (`MagizineDbContext`).
  - Unit of Work pattern (`SaveChangesAsync`).
  - Change tracking, transaction boundaries, and navigation property enforcement.
  - Generates application-level ULIDs (`Ulid.NewUlid().ToString()`) prior to persistence.
- **Read Path (Queries)**: Handled via Dapper (`DapperService`).
  - Executes stored procedures (`fn_get_article_list`, `fn_get_article_by_id`, etc.).
  - Total count and pagination computed in a single round-trip.
  - Zero EF tracking overhead, direct buffer mapping into response DTOs.

### 3.3 The ULID Identity Standard
All primary keys and foreign keys use **ULID** (Universally Unique Lexicographically Sortable Identifier) stored as `VARCHAR(26)`:
- **Format**: 26 characters, Crockford's Base32 alphabet (`0123456789ABCDEFGHJKMNPQRSTVWXYZ`).
- **Millisecond Precision**: The first 10 characters encode timestamp, ensuring natural B-Tree clustering on disk and chronologically sorted queries.
- **Cryptographic Randomness**: The remaining 16 characters provide 80 bits of cryptographic randomness per millisecond.
- **Security**: Completely prevents enumeration attacks (e.g., guessing `id=1, id=2`).
- **Zero Roundtrips**: IDs are created in-memory by the application before saving.

### 3.4 Security & Authentication Architecture
- **JWT Options Validation**: On application boot (`FeatureManager.AddJwtServices`), `JwtOptions.Validate()` ensures `Issuer`, `Audience`, and a secret key of at least 32 bytes (256 bits) exist. The API fails loudly at startup if misconfigured.
- **Claim Integrity**: Claims preserve RFC 7519 standards (`sub` contains the ULID user ID, `username`, `role`). Inbound claims mapping is explicitly disabled (`MapInboundClaims = false`).
- **Unified 401 & 403 Response**: Replaces the ASP.NET Core empty challenge body with `ErrorEnvelope`, ensuring frontend error parsing is uniform across all failure types.
- **CORS Defense-in-Depth**: Strict origin matching via `CorsOptions`. Empty by default, blocking unauthorized browser cross-origin requests until explicit origins are configured.
- **Password Hashing**: PBKDF2 with HMAC-SHA256, 100,000 iterations, 128-bit cryptographically secure salt.

### 3.5 Global Soft-Delete Strategy
Nine out of the ten tables include:
- `IsDeleted` (`BIT` / `BOOLEAN`, default `0` / `false`)
- `DeletedAt` (`DATETIME2` / `TIMESTAMPTZ`, nullable)
- `DeletedByAdminId` (`VARCHAR(26)`, nullable)

`MagizineDbContext` applies global LINQ query filters (`HasQueryFilter(e => !e.IsDeleted)`). Deleted entities are invisible to application queries unless explicitly queried using `.IgnoreQueryFilters()`.

---

## 4. Repository Structure & File Map

```
MagizineBlog/
├── Doc/                                    # Documentation, database designs & scaffolding scripts
│   ├── Issue_Tracker.xlsx                 # Issue and bug tracking register
│   ├── LEdition_Database_Schema.xlsx      # Database design sheet
│   ├── Magazine_Project_Architecture.xlsx # High-level architecture map
│   ├── Rescaffold.ps1                     # Safe EF Core reverse engineering automation
│   ├── SchemaMigration_PartialUniqueIndexes.sql # PostgreSQL index optimization
│   ├── Supabase_Schema.sql                # Baseline Supabase schema
│   └── Task_Tracker.xlsx                  # Project milestone tracker
│
├── Database_Scripts/                       # Full SQL Server and Dapper scripts
│   ├── 01_Dapper_StoredProcedures.sql     # Baseline Dapper reading procedures
│   ├── 02_Dapper_DetailProcedures.sql     # Single-item retrieval procedures
│   ├── 03_Dapper_RequestArticleProcedures.sql # Article pitch query procedures
│   ├── Full_Database_Script.sql           # Complete PostgreSQL DDL & functions
│   └── Full_Database_Script_SqlServer.sql # Complete SQL Server T-SQL script (All ULID, 10 SPs, indexes, seeds)
│
├── Magizine.DataBase/                      # Entity Framework Core Data Layer
│   ├── DependencyInjection.cs             # AddMagizineDatabase() registration helper
│   ├── MagizineDbContext.cs               # Derived DbContext applying soft-delete filters
│   ├── EFContext/                         # Scaffolded entity models & context
│   │   ├── AppDbContext.cs                # EF Core Fluent API model configuration
│   │   ├── TblAboutAndAppDatum.cs         # System settings entity
│   │   ├── TblAd.cs                       # Advertisement campaigns entity
│   │   ├── TblAdmin.cs                    # Administrators entity
│   │   ├── TblArticle.cs                  # Articles and blog posts entity
│   │   ├── TblArticleCategory.cs          # Article categories entity
│   │   ├── TblAuthor.cs                   # Contributing authors entity
│   │   ├── TblContactMessage.cs           # User contact messages entity
│   │   ├── TblNewsletter.cs               # Newsletter subscribers entity
│   │   ├── TblNotification.cs             # In-app notifications entity
│   │   └── TblRequestArticle.cs           # Freelance article pitches entity
│   └── Paging/                            # EF Core IQueryable paging helpers
│
├── Magizine.Shared/                        # Domain contracts, DTOs & Cross-cutting utilities
│   ├── Enums/                             # System enums (Role, Status, Placements, RespType)
│   ├── Exceptions/                        # Error envelopes, validation builders & mappers
│   ├── JsonResources/                     # Error message code constants
│   ├── Models/                            # Request & Response DTOs
│   │   ├── Article/ArticleModels.cs       # Article DTOs (List, Detail, Create, Update, Status)
│   │   ├── ArticleCategory/ArticleCategoryModels.cs # Category DTOs
│   │   ├── Author/AuthorModels.cs         # Author DTOs
│   │   ├── RequestArticle/RequestArticleModels.cs # Article pitch DTOs
│   │   ├── Paging/                        # PageRequest & PagedResult<T> models
│   │   └── Result.cs                      # Transport-agnostic Result<T> monad
│   ├── Security/                          # JWT generation, PasswordHasher, Claims, CORS options
│   └── Services/                          # DapperService stored-procedure execution engine
│
├── MagizineAdmin.Api/                      # Admin Back-Office API
│   ├── Features/                          # Vertical-Slice Feature Modules
│   │   ├── Article/                       # Article curation & publishing endpoints
│   │   ├── ArticleCategory/               # Category CRUD endpoints
│   │   ├── Author/                        # Author onboarding & management endpoints
│   │   └── RequestArticle/                # Pitch review endpoints
│   ├── FeatureManager.cs                  # Modular DI composition root
│   ├── GlobalExceptionHandler.cs          # Centralized error handler
│   ├── Program.cs                         # Pipeline configuration
│   └── Dockerfile                         # Container definition
│
├── MagizineAuthor.Api/                     # Author & Journalist Workspace API
│   ├── Features/
│   │   ├── Article/                       # Author drafts & submissions
│   │   └── ArticleCategory/               # Category lookup for writing
│   ├── FeatureManager.cs                  # Composition root
│   └── Program.cs                         # Pipeline configuration
│
├── MagizinePublic.Api/                     # Public Content Gateway API
│   ├── Features/
│   │   ├── Article/                       # Public article reader endpoints
│   │   ├── ArticleCategory/               # Public category navigation endpoints
│   │   └── RequestArticle/                # Public pitch submission endpoint
│   ├── FeatureManager.cs                  # Composition root
│   └── Program.cs                         # Pipeline configuration
│
├── Magizine.Tests/                         # xUnit Test Suite
│   ├── Enums/                             # Enum parsing tests
│   ├── Exceptions/                        # Error mapping tests
│   ├── Features/ArticleCategory/          # Unit & Integration tests for categories
│   ├── Paging/                            # Pagination math & slicing tests
│   └── Security/                          # JWT, password hasher, and CORS tests
│
├── MagizineBackend.slnx                    # XML-based modern .NET Solution File
├── PROJECT_DOCUMENTATION.md               # Master technical documentation
├── README.md                              # Repository overview & quick start
└── skill.sh                               # CLI automation and management script
```

---

## 5. API Endpoints Catalog

### 5.1 Public API (`MagizinePublic.Api` — Port 5080)
| Method | Route | Description | Auth | Request Body | Response Type |
|---|---|---|---|---|---|
| `GET` | `/api/public/article/list` | Retrieve published articles (paginated, filtered) | None | Query Params (`ArticleListReqModel`) | `Result<PagedResult<ArticleResModel>>` |
| `GET` | `/api/public/article/{slug}` | Get published article by unique slug | None | Route: `{slug}` | `Result<ArticleResModel>` |
| `GET` | `/api/public/article-category/list` | Retrieve all active categories | None | None | `Result<List<ArticleCategoryResModel>>` |
| `GET` | `/api/public/article-category/{slug}` | Get category detail by slug | None | Route: `{slug}` | `Result<ArticleCategoryResModel>` |
| `POST` | `/api/public/requestarticle/submit` | Submit an unsolicited article pitch | None | `CreateRequestArticleReqModel` | `Result<string>` |

### 5.2 Author API (`MagizineAuthor.Api` — Port 5267)
| Method | Route | Description | Auth | Request Body | Response Type |
|---|---|---|---|---|---|
| `POST` | `/api/author/article/List` | Get logged-in author's articles | Author | `ArticleListReqModel` | `Result<PagedResult<ArticleResModel>>` |
| `POST` | `/api/author/article/Detail` | Get author's article by ID | Author | `ArticleDetailReqModel` | `Result<ArticleResModel>` |
| `POST` | `/api/author/article/Create` | Create a new article draft | Author | `CreateArticleReqModel` | `Result<ArticleResModel>` |
| `POST` | `/api/author/article/Update` | Update existing draft | Author | `UpdateArticleReqModel` | `Result<ArticleResModel>` |
| `POST` | `/api/author/article/SubmitForReview` | Transition draft/rejected to `Pending` | Author | `ArticleDetailReqModel` | `Result<string>` |
| `GET` | `/api/author/article-category/list` | Category dropdown for article composition | Author | None | `Result<List<ArticleCategoryResModel>>` |

### 5.3 Admin API (`MagizineAdmin.Api` — Port 5024)
| Method | Route | Description | Auth | Request Body | Response Type |
|---|---|---|---|---|---|
| **Article Management** | | | | | |
| `POST` | `/api/admin/article/List` | Admin overview of all articles | Admin | `ArticleListReqModel` | `Result<PagedResult<ArticleResModel>>` |
| `POST` | `/api/admin/article/Detail` | Full article detail including audit info | Admin | `ArticleDetailReqModel` | `Result<ArticleResModel>` |
| `POST` | `/api/admin/article/Create` | Directly create an article as Admin | Admin | `CreateArticleReqModel` | `Result<ArticleResModel>` |
| `POST` | `/api/admin/article/Update` | Modify article content/metadata | Admin | `UpdateArticleReqModel` | `Result<ArticleResModel>` |
| `POST` | `/api/admin/article/ChangeStatus` | Approve, Publish, Reject, Spotlight, Feature | Admin | `ChangeArticleStatusReqModel` | `Result<string>` |
| `POST` | `/api/admin/article/Delete` | Soft-delete article | Admin | `ArticleDetailReqModel` | `Result<string>` |
| **Category Management** | | | | | |
| `POST` | `/api/admin/article-category/List` | Paginated category list | Admin | `PageRequest` | `Result<PagedResult<ArticleCategoryResModel>>` |
| `POST` | `/api/admin/article-category/Detail` | Category detail | Admin | `ArticleCategoryDetailReqModel` | `Result<ArticleCategoryResModel>` |
| `POST` | `/api/admin/article-category/Create` | Create category (auto slug) | Admin | `CreateArticleCategoryReqModel` | `Result<ArticleCategoryResModel>` |
| `POST` | `/api/admin/article-category/Update` | Update category name/slug/order | Admin | `UpdateArticleCategoryReqModel` | `Result<ArticleCategoryResModel>` |
| `POST` | `/api/admin/article-category/Delete` | Soft-delete category | Admin | `ArticleCategoryDetailReqModel` | `Result<string>` |
| **Author Management** | | | | | |
| `POST` | `/api/admin/author/List` | Paginated author list | Admin | `AuthorListReqModel` | `Result<PagedResult<AuthorResModel>>` |
| `POST` | `/api/admin/author/Detail` | Author profile detail | Admin | `AuthorDetailReqModel` | `Result<AuthorResModel>` |
| `POST` | `/api/admin/author/Create` | Onboard new author | Admin | `CreateAuthorReqModel` | `Result<AuthorResModel>` |
| `POST` | `/api/admin/author/Update` | Update author bio/socials | Admin | `UpdateAuthorReqModel` | `Result<AuthorResModel>` |
| `POST` | `/api/admin/author/ChangeStatus`| Suspend, Ban, or Activate author | Admin | `ChangeAuthorStatusReqModel` | `Result<string>` |
| `POST` | `/api/admin/author/Delete` | Soft-delete author | Admin | `AuthorDetailReqModel` | `Result<string>` |
| **Pitch Review** | | | | | |
| `POST` | `/api/admin/requestarticle/List` | List submitted pitches | Admin | `RequestArticleListReqModel` | `Result<PagedResult<RequestArticleResModel>>` |
| `POST` | `/api/admin/requestarticle/Detail` | Detailed pitch review | Admin | `RequestArticleDetailReqModel` | `Result<RequestArticleResModel>` |
| `POST` | `/api/admin/requestarticle/Review` | Approve or Reject pitch with notes | Admin | `ReviewRequestArticleReqModel` | `Result<string>` |

---

## 6. Code Style & Engineering Standards

### 6.1 Language & Framework Conventions
- **C# 13 Idioms**: Prefer target-typed expressions (`new()`), file-scoped namespaces (`namespace MagizineAdmin.Api.Features;`), and pattern matching.
- **Nullability**: Nullable reference types are strictly enforced (`<Nullable>enable</Nullable>`). Every parameter and property explicitly declares nullability (`string?` vs `string`).
- **Immutability in DTOs**: Request and response DTOs use init-only properties (`public string Title { get; init; } = string.Empty;`).

### 6.2 Serialization & Wire Contract
- **PascalCase Property Names**: Serialization configuration explicitly sets:
  ```csharp
  options.JsonSerializerOptions.PropertyNamingPolicy = null;
  ```
  All JSON properties on the wire match C# PascalCase (e.g., `ArticleId`, `RespCode`, `IsSuccess`).
- **String Enums**: Enums are serialized as names, not integers:
  ```csharp
  options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
  ```
  Ensures client code is never broken by enum member reordering.

### 6.3 Response Standard: The `Result<T>` Monad
Controllers never return raw entity objects. All service operations return `Result<T>`:
```csharp
// Success
return Result<ArticleResModel>.Success(data);

// Expected Business Error
return Result<ArticleResModel>.Error("Category not found.");

// Unhandled Exception Catch
catch (Exception ex)
{
    return Result<ArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error updating draft"));
}
```
`Result<T>.Error(ex, ...)` strips the raw exception message to protect internal architecture details while invoking the logging callback.

### 6.4 Standard Error Envelope
On non-200 HTTP responses, the body conforms to `ErrorEnvelope`:
```json
{
  "RespType": "Error",
  "RespCode": "401",
  "RespDesp": "Authentication is required to access this resource.",
  "TraceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00",
  "Errors": null
}
```

### 6.5 Dependency Injection & Composition Roots
No "walls of registrations" in `Program.cs`. Each API uses an extension method on `WebApplicationBuilder` inside `FeatureManager.cs`:
```csharp
builder.AddModularService()
       .AddDatabaseServices()
       .AddCorsServices()
       .AddJwtServices()
       .AddSecurityServices()
       .AddFeatureServices();
```

---

## 7. Setup & Usage Guide

### 7.1 Prerequisites
- **.NET 10.0 SDK** (verified with build 10.0.302)
- **SQL Server 2019+** or **PostgreSQL 15+ (Supabase)**
- **Bash** (Git Bash, WSL, or Linux shell) for executing `skill.sh`

### 7.2 Database Setup (SQL Server)
1. Open SQL Server Management Studio (SSMS) or Azure Data Studio.
2. Open [`Database_Scripts/Full_Database_Script_SqlServer.sql`](file:///c:/Users/User/Nexus/Magizine%20Blog/MagizineBackend/Database_Scripts/Full_Database_Script_SqlServer.sql).
3. Execute the script against your database (e.g. `MagizineDb`).
4. The script automatically:
   - Drops any existing tables in reverse foreign-key order.
   - Creates all 10 tables with `VARCHAR(26)` ULID primary and foreign keys.
   - Creates all 10 stored procedures for Dapper reading.
   - Generates essential indexes and seeds default system data.

### 7.3 Configuring User Secrets
Connection strings and JWT keys must never be committed to source control:
```bash
# Set database connection for each API (Active Instance: DESKTOP-BQIFN7K / MagizineDB)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=DESKTOP-BQIFN7K;Initial Catalog=MagizineDB;User ID=SA;Password=12345;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True;" --project MagizineAdmin.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=DESKTOP-BQIFN7K;Initial Catalog=MagizineDB;User ID=SA;Password=12345;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True;" --project MagizineAuthor.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=DESKTOP-BQIFN7K;Initial Catalog=MagizineDB;User ID=SA;Password=12345;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True;" --project MagizinePublic.Api

# Set JWT Signing Key (minimum 32 characters)
dotnet user-secrets set "Jwt:Key" "super_secret_magazine_production_signing_key_2026!" --project MagizineAdmin.Api
dotnet user-secrets set "Jwt:Key" "super_secret_magazine_production_signing_key_2026!" --project MagizineAuthor.Api
```

### 7.4 Database Scaffolding (When Tables Are Added or Changed)
The backend follows a **Database-First** workflow.
- **What is scaffolded**: ONLY the Entity classes (`Tbl*.cs`) and [`AppDbContext.cs`](file:///c:/Users/User/Nexus/Magizine%20Blog/MagizineBackend/Magizine.DataBase/EFContext/AppDbContext.cs) inside `Magizine.DataBase\EFContext`.
- **What is NOT touched**: DTO models in `Magizine.Shared\Models` remain completely untouched. Hand-crafted behavior in `MagizineDbContext.cs` (e.g. global soft-delete query filters) is also preserved.
- **Dynamic Support**: When you create a new table (e.g., `Tbl_Comment`, `Tbl_Tag`) or alter columns in SQL Server, running the scaffold command will automatically generate `TblComment.cs`, add `DbSet<TblComment>` to `AppDbContext.cs`, and update all entities without any hardcoded table limits.

#### Run Scaffolding:
```bash
# Option 1: Via skill.sh (One-click)
./skill.sh scaffold

# Option 2: Via PowerShell script (scaffold all tables)
powershell -ExecutionPolicy Bypass -File .\Doc\Rescaffold.ps1

# Option 3: Scaffold specific table(s) only
powershell -ExecutionPolicy Bypass -File .\Doc\Rescaffold.ps1 -Table "Tbl_Article", "Tbl_Tag"

# Option 4: Direct dotnet ef CLI command
dotnet ef dbcontext scaffold "Data Source=DESKTOP-BQIFN7K;Initial Catalog=MagizineDB;User ID=SA;Password=12345;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True;" Microsoft.EntityFrameworkCore.SqlServer \
    --project Magizine.DataBase/Magizine.DataBase.csproj \
    --startup-project MagizineAdmin.Api/MagizineAdmin.Api.csproj \
    --output-dir EFContext \
    --context AppDbContext \
    --namespace Magizine.DataBase.Models \
    --schema dbo \
    --no-onconfiguring \
    --force
```

### 7.5 Running the Applications
You can launch individual APIs using `dotnet run` or the `skill.sh` automation script:

```bash
# Using skill.sh
./skill.sh run-admin    # Starts Admin API on http://localhost:5024
./skill.sh run-author   # Starts Author API on http://localhost:5267
./skill.sh run-public   # Starts Public API on http://localhost:5080

# Or via dotnet CLI
dotnet run --project MagizineAdmin.Api --launch-profile http
dotnet run --project MagizineAuthor.Api --launch-profile http
dotnet run --project MagizinePublic.Api --launch-profile http
```

### 7.5 Interactive API Documentation
In Development mode, browse the interactive Scalar OpenAPI reference:
- Admin API: `http://localhost:5024/scalar/v1`
- Author API: `http://localhost:5267/scalar/v1`
- Public API: `http://localhost:5080/scalar/v1`
Raw OpenAPI specifications are available at `/openapi/v1.json`.

---

## 8. CLI Automation (`skill.sh`)

A dedicated executable management script, `skill.sh`, is located in the project root. It provides common developer workflows, validation, and maintenance tasks:

```bash
./skill.sh help          # Display command catalog
./skill.sh build         # Build the complete solution
./skill.sh clean         # Clean build artifacts (bin/obj)
./skill.sh test          # Run xUnit test suite
./skill.sh check-ulid    # Audit codebase for legacy BIGINT/long ID violations
./skill.sh run-admin     # Run MagizineAdmin.Api
./skill.sh run-author    # Run MagizineAuthor.Api
./skill.sh run-public    # Run MagizinePublic.Api
./skill.sh db-info       # View database scripts summary and stored procedure manifest
```
