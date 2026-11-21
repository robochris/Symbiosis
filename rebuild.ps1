# Rebuilds the generated scenes, prefabs and data. Unity must be closed.
$log = Join-Path $env:TEMP "shrunk-tests\rebuild.log"; New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
. (Join-Path $PSScriptRoot "unity-path.ps1")
$p = Start-Process -FilePath $unity -ArgumentList '-batchmode','-quit','-projectPath',$PSScriptRoot,'-executeMethod','Shrunk.EditorTools.RunSceneBuilder.BuildAll','-logFile',"`"$log`"" -WindowStyle Hidden -PassThru -Wait
"exit code: $($p.ExitCode)"
Select-String -Path $log -Pattern 'error CS|\[Shrunk\]|Exception' | Select-Object -First 30 | ForEach-Object { $_.Line }
