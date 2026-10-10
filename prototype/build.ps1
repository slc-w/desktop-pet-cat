$ErrorActionPreference = 'Stop'
$prototypeRoot = $PSScriptRoot
$frameworkRoot = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $frameworkRoot 'csc.exe'
$buildOutput = Join-Path $prototypeRoot 'bin'
New-Item -ItemType Directory -Force -Path $buildOutput | Out-Null
$references = @('System.dll', 'System.Core.dll', 'System.Web.Extensions.dll', 'System.Windows.Forms.dll', 'System.Drawing.dll', 'WPF\WindowsBase.dll', 'WPF\PresentationCore.dll', 'WPF\PresentationFramework.dll', 'System.Xaml.dll')
$compilerArgs = @('/nologo', '/target:winexe', '/platform:x64', '/optimize+', "/out:$buildOutput\DesktopCat.exe")
foreach ($reference in $references) { $compilerArgs += "/reference:$(Join-Path $frameworkRoot $reference)" }
$compilerArgs += @(Get-ChildItem -LiteralPath $prototypeRoot -Filter '*.cs' | ForEach-Object { $_.FullName })
& $compiler @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Output "Built $buildOutput\DesktopCat.exe"
