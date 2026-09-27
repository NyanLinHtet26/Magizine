# MagizineBackend

Backend for L'ÉDITION, a digital magazine platform. Two ASP.NET Core APIs (public and admin)
sharing a single EF Core data layer, backed by Supabase Postgres.

**Status:** the data layer is complete. The APIs currently expose **no business endpoints** —
there are no controllers yet, and authentication has not been started.

**Repository:** `github.com/NyanLinHtet26/Magizine` (public) — default branch `Magzine-dev`.

---

## Project layout

| Project | Role | Target | Key packages |
|---|---|---|---|
| `MagizineAuthor.Api` | Public, author-facing API | `net10.0` | `Microsoft.AspNetCore.OpenApi` 10.0.10 |
| `MagizineAdmin.Api` | Admin/back-office API | `net10.0` | `Microsoft.AspNetCore.OpenApi` 10.0.10 |
| `Magizine.DataBase` | EF Core data layer, referenced by both APIs | `net10.0` | `Microsoft.EntityFrameworkCore{,.Relational,.Design}` 10.0.12, `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 |

Both APIs call one shared registration helper, so the provider and connection-string key are
defined in exactly one place:

```csharp
builder.Services.AddMagizineDatabase(builder.Configuration);
```

`AddMagizineDatabase` lives in `Magizine.DataBase/DependencyInjection.cs` and registers
`MagizineDbContext`. It throws a descriptive error at startup if
`ConnectionStrings:DefaultConnection` is missing.

---

## Prerequisites

- .NET 10 SDK
- The EF Core CLI (one-off):
  ```powershell
  dotnet tool install --global dotnet-ef --version 10.0.12
  ```
- Access to the Supabase project

---

## Getting started

### 1. Supply the connection string

The connection string is **never** committed. It goes in user-secrets — set it for **both**
API projects:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<value>" --project ".\MagizineAuthor.Api\MagizineAuthor.Api.csproj"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<value>" --project ".\MagizineAdmin.Api\MagizineAdmin.Api.csproj"
```

> Use the **session pooler on port 5432**, not the transaction-mode pooler on 6543. See
> [Rules and gotchas](#rules-and-gotchas).

### 2. Run an API

```powershell
dotnet run --project .\MagizineAuthor.Api\MagizineAuthor.Api.csproj    # http://localhost:5267
dotnet run --project .\MagizineAdmin.Api\MagizineAdmin.Api.csproj     # http://localhost:5024
```

In `Development` the OpenAPI document is served at `/openapi/v1.json`.

---

## Database-first workflow

**The database is the source of truth.** There are no EF migrations in this project, and none
should ever be generated. Schema changes are made in Supabase (SQL editor or Studio), then the
C# model is regenerated to match.

### Regenerate the model

```powershell
.\Doc\Rescaffold.ps1
```

Expected output:

```
Scaffolding from the public schema ...
Pruning entity files that are not magazine tables ...

  DbSets : 10 (expected 10)
  Files  : 11 (expected 11)
OK - 10 magazine tables scaffolded, no credentials in generated code.
```

Then confirm the model is what you expect:

```powershell
git status --porcelain    # empty == model byte-identical, nothing changed
```

Re-running the script is a **true no-op** — it is verified to produce a byte-identical model.

### What the script actually does

1. Reads the connection string from `MagizineAdmin.Api`'s user-secrets.
2. Scaffolds **only the `public` schema**, which holds the 10 magazine tables. Supabase's own
   `auth` / `storage` / `realtime` tables are deliberately excluded.
3. Scaffolds *over* the existing files (`--force`) rather than deleting first. `dotnet ef`
   builds the project before scaffolding, and `DependencyInjection.cs` references the
   generated types — clearing the directory first would break the build.
4. Prunes any generated file that isn't one of the 10 magazine entities.
5. Asserts 10 DbSets / 11 files, and **fails** if `OnConfiguring` or a password appears in
   `AppDbContext.cs`.

### The underlying command

```powershell
$cs = ((dotnet user-secrets list --project .\MagizineAdmin.Api\MagizineAdmin.Api.csproj |
        Select-String 'ConnectionStrings:DefaultConnection') -split '=', 2)[1].Trim()

dotnet ef dbcontext scaffold $cs Npgsql.EntityFrameworkCore.PostgreSQL `
    --project Magizine.DataBase\Magizine.DataBase.csproj `
    --output-dir EFContext `
    --context AppDbContext `
    --namespace Magizine.DataBase.Models `
    --schema public `
    --no-onconfiguring `
    --force
```

Prefer the script — it also prunes leftovers and guards against credential leakage. If you run
this by hand you must prune the orphaned entity files yourself.

### The 10 magazine tables

| Table | Entity class | Soft delete |
|---|---|---|
| `Tbl_Admin` | `TblAdmin` | yes |
| `Tbl_Author` | `TblAuthor` | yes |
| `Tbl_Article` | `TblArticle` | yes |
| `Tbl_ArticleCategory` | `TblArticleCategory` | yes |
| `Tbl_RequestArticle` | `TblRequestArticle` | yes |
| `Tbl_ContactMessage` | `TblContactMessage` | yes |
| `Tbl_Newsletter` | `TblNewsletter` | yes |
| `Tbl_Notification` | `TblNotification` | yes |
| `Tbl_Ads` | `TblAd` | yes |
| `Tbl_AboutAndAppData` | `TblAboutAndAppDatum` | **no** — has no soft-delete columns |

Schema reference: `Doc/Supabase_Schema.sql` plus the `.xlsx` design documents in `Doc/`.

---

## Soft deletes

`Magizine.DataBase/MagizineDbContext.cs` derives from the scaffolded `AppDbContext` and adds a
`!IsDeleted` query filter to the 9 tables that support it. Rows with `IsDeleted = true` are
excluded from ordinary queries automatically.

```csharp
modelBuilder.Entity<TblArticle>().HasQueryFilter(e => !e.IsDeleted);
```

This file lives **outside** `EFContext/` on purpose, so re-scaffolding can never clobber it.
Hand-written behaviour goes there; scaffolded mappings go in `EFContext/`.

---

## Adding or renaming a table

The script carries a hardcoded allowlist of the 10 expected tables. If you add an 11th table
(or rename one) in Supabase, the script will scaffold it, then **prune the new file** because
its class name isn't in the allowlist — leaving a broken build. That is a deliberate guard, but
it means you must edit the script.

In `Doc/Rescaffold.ps1`, add the table to **both** arrays:

```powershell
$expectedTableNames = @( ... 'Tbl_YourNewTable' )    # schema name
$expectedClassNames = @( ... 'TblYourNewTable' )      # EF's singularised class name
```

Then re-run. Both arrays must stay the same length as each other.

---

## Rules and gotchas

- **Never run `dotnet ef migrations add`.** The model still contains 12 `auth`/`realtime`/`storage`
  enums and 4 Postgres extensions, because EF discovers those at *database* level — restricting
  to the `public` schema filters tables but not enums or extensions. A migration would try to
  create them. Make schema changes in Supabase instead.
- **Never omit `--no-onconfiguring`.** Without it EF writes a hardcoded connection string into
  `AppDbContext.cs`, and that `OnConfiguring` override silently takes precedence over the
  connection string registered by `AddMagizineDatabase` — putting your password in source.
- **Use the session pooler (port 5432), not transaction mode (6543).** In transaction mode
  Supavisor serves exactly one query per connection and every subsequent read on that connection
  stalls until it times out. This presents as a mysterious ~32 s `TimeoutException` on the
  *second* query in a request, not as an auth error.
- **The scaffold script reads the secret from `MagizineAdmin.Api` only** — never from
  `MagizineAuthor.Api`. Both must be updated whenever the password changes.
- **Never commit credentials.** Connection strings live in user-secrets; `.gitignore` also
  excludes `appsettings.Local.json`, `.env`, and `secrets.json`.
- **In production**, supply the connection string as the environment variable
  `ConnectionStrings__DefaultConnection` (double underscore).

---

## Known issues

- **4 build warnings: `NU1903`.** `Microsoft.OpenApi` 2.0.0, pulled in transitively by
  `Microsoft.AspNetCore.OpenApi` 10.0.10, has a known high-severity advisory
  ([GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc)). Currently
  accepted deliberately; needs an explicit package upgrade to clear.
- **The published history is a single commit containing no credentials.** The pre-rotation
  commit `88e8892` exists only on the local `master` and `archive-full-history` branches and was
  never pushed. Don't `git push --all`, or push either branch to the public remote.
- `Doc/` contains design `.xlsx` files and `Supabase_Schema.sql`. Copies of these also exist
  outside the repository; the committed copies in `Doc/` are the ones in use.
