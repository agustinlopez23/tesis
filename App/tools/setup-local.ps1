$ErrorActionPreference = 'Stop'
$appRoot = Split-Path $PSScriptRoot -Parent
$localFile = Join-Path $appRoot '.env.local'
if (-not (Test-Path -LiteralPath $localFile)) {
    $randomBytes = New-Object byte[] 32
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $generator.GetBytes($randomBytes)
    $generator.Dispose()
    $generatedPassword = [BitConverter]::ToString($randomBytes).Replace('-', '')
    [IO.File]::WriteAllText($localFile, "POSTGRES_PASSWORD=$generatedPassword`n")
}
Write-Output 'Configuración local disponible en App/.env.local. No se modifican credenciales existentes.'
