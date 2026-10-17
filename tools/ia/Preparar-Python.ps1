param([Parameter(Mandatory=$true)][string]$Python311,[switch]$Referencia)
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskArgs=@((Join-Path $taskRoot 'IA/preparar.py'))
if($Referencia){$taskArgs+='--referencia'}
& $Python311 @taskArgs
if($LASTEXITCODE -ne 0){throw 'No se pudo preparar IA. Se requiere Python 3.11 y acceso a los paquetes fijados.'}
