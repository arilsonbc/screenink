param([string]$OutputName="ScreenInk-0.2.0-final.exe")
$ErrorActionPreference='Stop'
$projectDir=$PSScriptRoot
New-Item -ItemType Directory -Force (Join-Path $projectDir 'app') | Out-Null
$arguments=@('/nologo','/target:winexe','/optimize+',('/out:'+(Join-Path $projectDir ('app/'+$OutputName))),('/win32icon:'+(Join-Path $projectDir 'branding/app.ico')),'/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll','/reference:System.IO.Compression.dll','/reference:System.IO.Compression.FileSystem.dll')
foreach($name in @('marker','history','eraser','screenshot','trash')){$arguments+=('/resource:'+(Join-Path $projectDir ('src/icons/'+$name+'.png'))+','+$name+'.png')}
$arguments+=@(Join-Path $projectDir 'src/ScreenInk.cs';Join-Path $projectDir 'src/Advanced.cs')
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') @arguments
if($LASTEXITCODE -ne 0){throw 'Compilação falhou'}



