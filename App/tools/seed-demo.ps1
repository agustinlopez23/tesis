# Loads the demo seed into the LOCAL workspace database (App/.env.local, port 55432).
# Generates distinct owner/admin passwords on first run and keeps them in .tools/seed-demo/credentials.json.
# Re-running is safe: existing data is preserved and the two demo promotions get a fresh 30-day window.
# Hosted demo database (e.g. Supabase): this script never targets it. Run Marketplace.Seed directly with
# --apply --allow-hosted-demo and DEMO_SEED_CONFIRM_HOST=<exact host>; see App/seed/README.md (bash example included).
$ErrorActionPreference = 'Stop'
$seedRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$seedDirectory = Join-Path $seedRoot '.tools/seed-demo'
$credentialPath = Join-Path $seedDirectory 'credentials.json'
$seedVariables = @('DOTNET_ROOT','DOTNET_CLI_HOME','ConnectionStrings__Marketplace','ASPNETCORE_ENVIRONMENT','SEED_OWNER_PASSWORD','SEED_ADMIN_PASSWORD')
$previous = @{}
foreach ($name in $seedVariables) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    New-Item -ItemType Directory -Path $seedDirectory -Force | Out-Null
    if (-not (Test-Path -LiteralPath $credentialPath)) {
        $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
        try {
            $bytes = New-Object byte[] 24
            $generator.GetBytes($bytes); $ownerPassword = [Convert]::ToBase64String($bytes)
            $generator.GetBytes($bytes); $adminPassword = [Convert]::ToBase64String($bytes)
        } finally { $generator.Dispose() }
        $credentials = @{ owner=@{ email='comerciante.seed@example.test'; password=$ownerPassword }; admin=@{ email='admin.seed@example.test'; password=$adminPassword } }
        $json = $credentials | ConvertTo-Json -Depth 3
        $stream = [IO.File]::Open($credentialPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try { $encoded = [Text.UTF8Encoding]::new($false).GetBytes($json); $stream.Write($encoded, 0, $encoded.Length) } finally { $stream.Dispose() }
    }
    $credentials = Get-Content -Raw -Encoding utf8 -LiteralPath $credentialPath | ConvertFrom-Json
    $passwordLine = Get-Content -LiteralPath (Join-Path $seedRoot 'App/.env.local') | Where-Object { $_.StartsWith('POSTGRES_PASSWORD=') }
    if (-not $passwordLine) { throw 'Falta POSTGRES_PASSWORD en App/.env.local.' }
    $env:ConnectionStrings__Marketplace = "Host=127.0.0.1;Port=55432;Database=marketplace;Username=marketplace;Password=$($passwordLine.Substring(18))"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:SEED_OWNER_PASSWORD = $credentials.owner.password
    $env:SEED_ADMIN_PASSWORD = $credentials.admin.password
    $dotnet = Join-Path $seedRoot '.tools/dotnet/dotnet.exe'
    if (Test-Path -LiteralPath $dotnet) { $env:DOTNET_ROOT = Split-Path $dotnet -Parent }
    else { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
    $env:DOTNET_CLI_HOME = Join-Path $seedRoot '.tools/cli'
    & $dotnet run --project (Join-Path $seedRoot 'App/backend/tools/Marketplace.Seed') --configuration Release -- --apply
    if ($LASTEXITCODE -ne 0) { throw 'El seed no se completó. Las credenciales se conservan para reintentar.' }
    Write-Output "Credenciales locales: $credentialPath"
    Write-Output 'Abrí http://localhost:3000/mi-negocio para ingresar.'
} finally {
    foreach ($name in $seedVariables) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
}
