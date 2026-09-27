<#
.SYNOPSIS
    Regenerates the EF Core model in Magizine.DataBase\EFContext from the live Supabase
    database. The database is the source of truth (database-first workflow); this script
    is the only sanctioned way to refresh the model.

.DESCRIPTION
    Reads the connection string from user-secrets (never a literal in source) and scaffolds
    ONLY the "public" schema, which contains the 10 magazine tables. Supabase's own tables
    live in the auth / storage / realtime schemas and are deliberately excluded - modelling
    them would make the context imply it owns part of Supabase's internal schema.

    --no-onconfiguring is load-bearing. Without it EF writes a hardcoded connection string
    into AppDbContext.cs, and that OnConfiguring override silently takes precedence over the
    connection string registered by AddMagizineDatabase.

    Order matters: this script scaffolds *over* the existing files and prunes the leftovers
    afterwards. Deleting EFContext first would deadlock, because "dotnet ef" builds the
    project before scaffolding and DependencyInjection.cs references the generated types.

.EXAMPLE
    .\Doc\Rescaffold.ps1
#>
[CmdletBinding()]
param(
    [switch]$UseTableWhitelist
)

$ErrorActionPreference = 'Stop'

$repoRoot       = Split-Path -Parent $PSScriptRoot
$dataProject    = Join-Path $repoRoot 'Magizine.DataBase\Magizine.DataBase.csproj'
$secretsProject = Join-Path $repoRoot 'MagizineAdmin.Api\MagizineAdmin.Api.csproj'
$efContextDir   = Join-Path $repoRoot 'Magizine.DataBase\EFContext'
$dbContextFile  = Join-Path $efContextDir 'AppDbContext.cs'

# The ten magazine tables and the entity class names EF derives from them.
$expectedTableNames = @(
    'Tbl_Admin', 'Tbl_Author', 'Tbl_Article', 'Tbl_ArticleCategory',
    'Tbl_RequestArticle', 'Tbl_ContactMessage', 'Tbl_Newsletter',
    'Tbl_AboutAndAppData', 'Tbl_Ads', 'Tbl_Notification'
)
$expectedClassNames = @(
    'AppDbContext', 'TblAdmin', 'TblAuthor', 'TblArticle', 'TblArticleCategory',
    'TblRequestArticle', 'TblContactMessage', 'TblNewsletter', 'TblAboutAndAppDatum',
    'TblAd', 'TblNotification'
)
$expectedDbSetCount = $expectedTableNames.Count

# "dotnet user-secrets list" prints "Key = Value" lines by default (its --json output is
# wrapped in //BEGIN / //END markers). The value itself contains "=" characters, so split on
# the first separator only.
$secretLine = dotnet user-secrets list --project $secretsProject |
    Where-Object { $_ -like 'ConnectionStrings:DefaultConnection*' } |
    Select-Object -First 1

$connectionString = if ($secretLine) { ($secretLine -split '=', 2)[1].Trim() } else { $null }

if ([string]::IsNullOrWhiteSpace($connectionString)) {
    throw "ConnectionStrings:DefaultConnection not found in user-secrets for $secretsProject. Set it with: dotnet user-secrets set `"ConnectionStrings:DefaultConnection`" `"<value>`" --project `"$secretsProject`""
}

$tableOrSchemaArg = if ($UseTableWhitelist) {
    @('-t') + ($expectedTableNames | ForEach-Object { "public.$_" })
} else {
    @('--schema', 'public')
}

Push-Location $repoRoot
try {
    Write-Host 'Scaffolding from the public schema ...' -ForegroundColor Cyan

    # Native commands write progress to stderr, which $ErrorActionPreference='Stop' would
    # turn into a terminating error, so relax it around the call and check $LASTEXITCODE.
    $ErrorActionPreference = 'Continue'
    & dotnet ef dbcontext scaffold $connectionString 'Npgsql.EntityFrameworkCore.PostgreSQL' `
        --project $dataProject `
        --output-dir 'EFContext' `
        --context 'AppDbContext' `
        --namespace 'Magizine.DataBase.Models' `
        $tableOrSchemaArg `
        '--no-onconfiguring' `
        --force
    $scaffoldExitCode = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'

    if ($scaffoldExitCode -ne 0) {
        throw "dotnet ef dbcontext scaffold failed with exit code $scaffoldExitCode"
    }

    Write-Host 'Pruning entity files that are not magazine tables ...' -ForegroundColor Cyan
    Get-ChildItem $efContextDir -Filter *.cs | ForEach-Object {
        if ($expectedClassNames -notcontains $_.BaseName) {
            Write-Host "  pruned $($_.Name)"
            Remove-Item $_.FullName -Force
        }
    }

    $dbSetCount = (Select-String -Path $dbContextFile -Pattern 'public virtual DbSet<' -AllMatches).Count
    $fileCount  = (Get-ChildItem $efContextDir -Filter *.cs).Count

    Write-Host ''
    Write-Host "  DbSets : $dbSetCount (expected $expectedDbSetCount)"
    Write-Host "  Files  : $fileCount (expected $($expectedClassNames.Count))"

    if ($dbSetCount -ne $expectedDbSetCount -or $fileCount -ne $expectedClassNames.Count) {
        Write-Warning "Unexpected scaffold shape. If --schema public did not restrict as intended, re-run with:"
        Write-Warning "    .\Doc\Rescaffold.ps1 -UseTableWhitelist"
        exit 1
    }

    $leak = Select-String -Path $dbContextFile -Pattern 'OnConfiguring|Password=' -ErrorAction SilentlyContinue
    if ($leak) {
        Write-Error 'AppDbContext.cs contains an OnConfiguring block or a password. The model is not safe to commit.'
        exit 1
    }

    Write-Host 'OK - 10 magazine tables scaffolded, no credentials in generated code.' -ForegroundColor Green
} finally {
    Pop-Location
}
