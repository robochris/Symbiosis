# Builds the game as a Windows program: Builds\Windows\Shrunk.exe. Unity must be closed.
$log = Join-Path $env:TEMP "shrunk-tests\build.log"; New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
$p = Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.0.55f1\Editor\Unity.exe" -ArgumentList '-batchmode','-quit','-projectPath',$PSScriptRoot,'-executeMethod','Shrunk.EditorTools.BuildGame.BuildWindows','-logFile',"`"$log`"" -PassThru -Wait
"exit code: $($p.ExitCode)"
Select-String -Path $log -Pattern 'error CS|\[Shrunk\]|Build failed|Exception:' | Select-Object -First 30 | ForEach-Object { $_.Line }
$exe = Join-Path $PSScriptRoot "Builds\Windows\Shrunk.exe"
if (Test-Path $exe) { "built: $exe  ($((Get-Item $exe).LastWriteTime))" } else { "no exe was produced" }
