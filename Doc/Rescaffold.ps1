<#
.SYNOPSIS
    Regenerates the EF Core Entity classes (Tbl*.cs) and AppDbContext.cs in Magizine.DataBase\EFContext
    from the live Microsoft SQL Server database (DESKTOP-BQIFN7K / MagizineDB).

.DESCRIPTION
    Scaffolds all tables in the 'dbo' schema (or explicitly specified tables) directly into
    Magizine.DataBase\EFContext. 

    - Only generates Entity classes (Tbl*.cs) and AppDbContext.cs.
    - Does NOT touch or overwrite DTO models in Magizine.Shared\Models.
    - Dynamically supports adding new tables or changing existing tables in SQL Server.
    - Enforces --no-onconfiguring so connection strings/credentials never leak into source code.
    - Automatically cleans up any non-entity artifacts while preserving all Tbl*.cs files.

.EXAMPLE
    .\Doc\Rescaffold.ps1
    .\Doc\Rescaffold.ps1 -Table "Tbl_Article", "Tbl_Category"
#>
[CmdletBinding()]
param(
    [string[]]$Table
)

$ErrorActionPreference = 'Stop'

$repoRoot       = Split-Path -Parent $PSScriptRoot
$dataProject    = Join-Path $repoRoot 'Magizine.DataBase\Magizine.DataBase.csproj'
$secretsProject = Join-Path $repoRoot 'MagizineAdmin.Api\MagizineAdmin.Api.csproj'
$efContextDir   = Join-Path $repoRoot 'Magizine.DataBase\EFContext'
$dbContextFile  = Join-Path $efContextDir 'AppDbContext.cs'

# Read connection string from user-secrets
$secretLine = dotnet user-secrets list --project $secretsProject |
    Where-Object { $_ -like 'ConnectionStrings:DefaultConnection*' } |
    Select-Object -First 1

$connectionString = if ($secretLine) { ($secretLine -split '=', 2)[1].Trim() } else { $null }

if ([string]::IsNullOrWhiteSpace($connectionString)) {
    throw "ConnectionStrings:DefaultConnection not found in user-secrets for $secretsProject. Set it with: dotnet user-secrets set `"ConnectionStrings:DefaultConnection`" `"<value>`" --project `"$secretsProject`""
}

# If specific tables are passed, scaffold those; otherwise scaffold the entire dbo schema
$tableOrSchemaArg = if ($Table -and $Table.Count -gt 0) {
    $Table | ForEach-Object { @('-t', $_) }
} else {
    @('--schema', 'dbo')
}

Push-Location $repoRoot
try {
    Write-Host "Scaffolding Entities (Tbl*.cs) and AppDbContext.cs from Microsoft SQL Server..." -ForegroundColor Cyan

    $ErrorActionPreference = 'Continue'
    & dotnet ef dbcontext scaffold $connectionString 'Microsoft.EntityFrameworkCore.SqlServer' `
        --project $dataProject `
        --startup-project $secretsProject `
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

    # Ensure only AppDbContext.cs and entity files (Tbl*.cs) remain in EFContext
    Get-ChildItem $efContextDir -Filter *.cs | ForEach-Object {
        if ($_.BaseName -ne 'AppDbContext' -and -not ($_.BaseName -like 'Tbl*')) {
            Write-Host "  Pruned non-entity file: $($_.Name)" -ForegroundColor Yellow
            Remove-Item $_.FullName -Force
        }
    }

    $dbSetCount = (Select-String -Path $dbContextFile -Pattern 'public virtual DbSet<' -AllMatches).Count
    $entityFiles = (Get-ChildItem $efContextDir -Filter 'Tbl*.cs').Count

    Write-Host ''
    Write-Host "  DbSets in AppDbContext : $dbSetCount" -ForegroundColor Green
    Write-Host "  Entity files (Tbl*.cs) : $entityFiles" -ForegroundColor Green

    # Security check: verify no password or connection string was scaffolded into AppDbContext.cs
    $leak = Select-String -Path $dbContextFile -Pattern 'OnConfiguring|Password=' -ErrorAction SilentlyContinue
    if ($leak) {
        Write-Error 'AppDbContext.cs contains an OnConfiguring block or a password. The model is not safe to commit.'
        exit 1
    }

    Write-Host '✔ Scaffolding complete! Entity models and AppDbContext.cs are synchronized with SQL Server.' -ForegroundColor Green
} finally {
    Pop-Location
}
