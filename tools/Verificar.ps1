param([switch]$Rendimiento)
$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $projectRoot
try {
    & dotnet test 'Sistema3S.Tests/Sistema3S.Tests.csproj' --logger 'trx;LogFileName=verificacion.trx'
    if ($LASTEXITCODE -ne 0) { throw 'Las pruebas .NET no pasaron.' }
    Push-Location (Join-Path $projectRoot 'Sistema3S.Client')
    try {
        & npm ci
        if ($LASTEXITCODE -ne 0) { throw 'npm ci falló.' }
        & npm run test -- --watch=false
        if ($LASTEXITCODE -ne 0) { throw 'Las pruebas Angular no pasaron.' }
        & npm run build -- --configuration production
        if ($LASTEXITCODE -ne 0) { throw 'Angular no compiló.' }
        & npm audit --json | Set-Content -LiteralPath (Join-Path $projectRoot 'npm-audit-verificacion.json')
        if ($LASTEXITCODE -ne 0) { Write-Warning 'Revisar los avisos de dependencias en npm-audit-verificacion.json; no ejecutar audit fix indiscriminadamente.' }
    } finally { Pop-Location }
    if ($Rendimiento) { Write-Host 'La medición controlada se realiza con el protocolo del informe; no cargar datos de rendimiento en BD_3S.' }
} finally { Pop-Location }
