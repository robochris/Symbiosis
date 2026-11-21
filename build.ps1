# Builds the game as a Windows program: Builds\Windows\Shrunk.exe. Unity must be closed.
$log = Join-Path $env:TEMP "shrunk-tests\build.log"; New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
. (Join-Path $PSScriptRoot "unity-path.ps1")
$p = Start-Process -FilePath $unity -ArgumentList '-batchmode','-quit','-projectPath',$PSScriptRoot,'-executeMethod','Shrunk.EditorTools.BuildGame.BuildWindows','-logFile',"`"$log`"" -PassThru -Wait
"exit code: $($p.ExitCode)"
Select-String -Path $log -Pattern 'error CS|\[Shrunk\]|Build failed|Exception:' | Select-Object -First 30 | ForEach-Object { $_.Line }
$exe = Join-Path $PSScriptRoot "Builds\Windows\Shrunk.exe"
if (Test-Path $exe) { "built: $exe  ($((Get-Item $exe).LastWriteTime))" } else { "no exe was produced" }
