param([Parameter(Mandatory=$true)][string]$Python311,[string]$Salida='')
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
& $Python311 (Join-Path $taskRoot 'IA/preparar.py') --referencia
if($LASTEXITCODE -ne 0){throw 'Falló la preparación del motor local. La carga no se inició.'}
if(!$Salida){$Salida=Join-Path $taskRoot 'IA/evidencia/demostracion'}
& dotnet run --project (Join-Path $PSScriptRoot 'HistorialDemostracion/HistorialDemostracion.csproj') -- ejecutar $Salida
if($LASTEXITCODE -ne 0){throw 'La carga o el procesamiento no se completó. Revisa la evidencia antes de reintentar.'}
Write-Output ('Referencia procesada. Evidencia local: '+$Salida)
