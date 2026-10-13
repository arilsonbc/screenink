$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'O compilador .NET Framework 4.x não foi encontrado.' }
$outDir = Join-Path $projectRoot 'dist'
New-Item -ItemType Directory -Force $outDir | Out-Null
$compilerArgs = @('/nologo','/target:winexe','/optimize+',('/out:' + (Join-Path $outDir 'Pointofix-Modern.exe')),'/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll')
foreach ($iconName in @('marker','history','eraser','screenshot','trash')) {
    $iconPath = Join-Path $projectRoot ('src/icons/' + $iconName + '.png')
    $compilerArgs += '/resource:' + $iconPath + ',' + $iconName + '.png'
}
$compilerArgs += Join-Path $projectRoot 'src/PointofixModern.cs'
& $compiler @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Falha na compilação.' }
Write-Output 'Aplicativo criado em dist/Pointofix-Modern.exe'
