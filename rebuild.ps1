# Rebuilds the generated scenes, prefabs and data. Unity must be closed.
$log = Join-Path $env:TEMP "shrunk-tests\rebuild.log"; New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
$p = Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.0.55f1\Editor\Unity.exe" -ArgumentList '-batchmode','-quit','-projectPath',$PSScriptRoot,'-executeMethod','Shrunk.EditorTools.RunSceneBuilder.BuildAll','-logFile',"`"$log`"" -PassThru -Wait
"exit code: $($p.ExitCode)"
Select-String -Path $log -Pattern 'error CS|\[Shrunk\]|Exception' | Select-Object -First 30 | ForEach-Object { $_.Line }
