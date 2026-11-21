# Builds the game for Android phones: Builds\Android\Shrunk.apk. Unity must be closed. The first build is slow.
$log = Join-Path $env:TEMP "shrunk-tests\build-android.log"; New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
. (Join-Path $PSScriptRoot "unity-path.ps1")
$p = Start-Process -FilePath $unity -ArgumentList '-batchmode','-quit','-projectPath',$PSScriptRoot,'-buildTarget','Android','-executeMethod','Shrunk.EditorTools.BuildGame.BuildAndroid','-logFile',"`"$log`"" -PassThru -Wait
"exit code: $($p.ExitCode)"
Select-String -Path $log -Pattern 'error CS|\[Shrunk\]|Build failed|Exception:|CommandInvokationFailure|Gradle' | Select-Object -First 30 | ForEach-Object { $_.Line }
$apk = Join-Path $PSScriptRoot "Builds\Android\Shrunk.apk"
if (Test-Path $apk) { "built: $apk  ($([math]::Round((Get-Item $apk).Length/1MB)) MB, $((Get-Item $apk).LastWriteTime))" } else { "no apk was produced" }
