# L'ÉDITION — MagizineBackend

Backend solution for **L'ÉDITION**, an enterprise digital magazine and journalistic publishing platform. Built with **.NET 10 (C# 13)**, featuring a modular vertical-slice feature architecture with CQRS read/write split, universal 26-character **ULID** identities, and multi-API gateways for Readers, Authors, and Administrators.

> 📖 **Comprehensive Documentation**: For full technical specifications, architecture diagrams, database schema, endpoint catalog, and code styles, see [**`PROJECT_DOCUMENTATION.md`**](PROJECT_DOCUMENTATION.md).

---

## Solution Overview

The solution consists of three specialized API gateways sharing a unified data and domain layer:

| Project | Role | Port (HTTP/HTTPS) | Description |
|---|---|---|---|
| **`MagizinePublic.Api`** | Reader Gateway | `5080` / `7080` | Public article feed, slug lookup, categories, and pitch submission. |
| **`MagizineAuthor.Api`** | Author Gateway | `5267` / `7137` | Author draft workspace, draft revisions, and submission for review. |
| **`MagizineAdmin.Api`** | Admin Gateway | `5024` / `7036` | Editorial governance, curation, categories, authors, and pitch reviews. |
| **`Magizine.DataBase`** | Command Layer | — | EF Core 10 data layer with global soft-delete query filters. |
| **`Magizine.Shared`** | Query & Domain | — | Dapper stored-procedure runner (`DapperService`), `Result<T>` monad, JWT auth, DTOs. |
| **`Magizine.Tests`** | Test Suite | — | xUnit, Moq, FluentAssertions unit and integration tests. |

---

## Key Architecture Highlights

- **CQRS Read/Write Split**:
  - **Writes / Commands**: Handled by **Entity Framework Core 10** (`MagizineDbContext`) with automated soft-delete query filtering.
  - **Reads / Queries**: Handled by **Dapper 2.1** (`DapperService`) via pre-compiled T-SQL / PostgreSQL stored procedures for single-trip paging and maximum throughput.
- **Universal ULID Identity**:
  - All Primary and Foreign Keys use 26-character Crockford Base32 **ULIDs** (`VARCHAR(26)` in SQL, `string` in C#).
  - Lexicographically sortable by millisecond timestamp; zero database roundtrips for key generation.
- **Dual Database Support**:
  - **SQL Server**: Fully automated via [`Database_Scripts/Full_Database_Script_SqlServer.sql`](Database_Scripts/Full_Database_Script_SqlServer.sql) with all 10 tables, 10 stored procedures, indices, and seed data.
  - **PostgreSQL / Supabase**: Supported via [`Doc/Supabase_Schema.sql`](Doc/Supabase_Schema.sql) and [`Doc/Rescaffold.ps1`](Doc/Rescaffold.ps1).
- **Transport Standard**:
  - Consistent `Result<T>` response model and standardized `ErrorEnvelope`.
  - PascalCase JSON wire serialization with string enum conversion.
  - Strict RFC 7519 JWT Bearer authentication with HMAC-SHA256.

---

## Quick Start

### 1. Database Setup (SQL Server)
Execute [`Database_Scripts/Full_Database_Script_SqlServer.sql`](Database_Scripts/Full_Database_Script_SqlServer.sql) in SQL Server Management Studio or Azure Data Studio.

### 2. Configure Secrets
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=DESKTOP-BQIFN7K;Initial Catalog=MagizineDB;User ID=SA;Password=12345;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True;" --project MagizineAdmin.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=DESKTOP-BQIFN7K;Initial Catalog=MagizineDB;User ID=SA;Password=12345;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True;" --project MagizineAuthor.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=DESKTOP-BQIFN7K;Initial Catalog=MagizineDB;User ID=SA;Password=12345;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True;" --project MagizinePublic.Api
```

### 3. Database Scaffolding (When Tables Are Added or Changed)
Scaffolding generates **ONLY** entity classes (`Tbl*.cs`) and `AppDbContext.cs` inside `Magizine.DataBase\EFContext`. Hand-crafted DTO models in `Magizine.Shared\Models` are never touched:
```bash
./skill.sh scaffold
```
Or via PowerShell:
```powershell
.\Doc\Rescaffold.ps1
```

### 4. Build & Run with `skill.sh`
```bash
# Build solution
./skill.sh build

# Run APIs
./skill.sh run-admin     # http://localhost:5024/scalar/v1
./skill.sh run-author    # http://localhost:5267/scalar/v1
./skill.sh run-public    # http://localhost:5080/scalar/v1
```

Or run directly with the `dotnet` CLI:
```bash
dotnet build
dotnet run --project MagizineAdmin.Api --launch-profile http
```

---

## Developer Tooling (`skill.sh`)

Use the included `./skill.sh` CLI script for common operations:

| Command | Action |
|---|---|
| `./skill.sh help` | Display available commands |
| `./skill.sh build` | Build all projects in the solution |
| `./skill.sh clean` | Clean all bin/obj directories |
| `./skill.sh test` | Run test suite with optimal timeout and network configuration |
| `./skill.sh check-ulid` | Scan the codebase for legacy `long` ID usages |
| `./skill.sh run-admin` | Launch `MagizineAdmin.Api` |
| `./skill.sh run-author` | Launch `MagizineAuthor.Api` |
| `./skill.sh run-public` | Launch `MagizinePublic.Api` |
| `./skill.sh db-info` | Display database script summary and procedure manifest |
