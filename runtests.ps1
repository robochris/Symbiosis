param([string]$Platform = "PlayMode", [string]$Filter = "")
$d = Join-Path $env:TEMP "shrunk-tests"; New-Item -ItemType Directory -Force $d | Out-Null
$log = "$d\$Platform.log"; $res = "$d\$Platform.xml"; Remove-Item $res -ErrorAction SilentlyContinue
. (Join-Path $PSScriptRoot "unity-path.ps1")
$a = @('-batchmode','-projectPath',$PSScriptRoot,'-runTests','-testPlatform',$Platform,'-testResults',"`"$res`"",'-logFile',"`"$log`"")
if ($Filter) { $a += @('-testFilter',$Filter) }
$p = Start-Process -FilePath $unity -ArgumentList $a -PassThru -Wait
if (Test-Path $res) {
  [xml]$x = Get-Content $res; $r = $x.'test-run'
  "$Platform : total=$($r.total) passed=$($r.passed) failed=$($r.failed) duration=$([math]::Round([double]$r.duration,1))s"
  $x.SelectNodes('//test-case') | ForEach-Object { "$($_.result)  $($_.name)  $([math]::Round([double]$_.duration,2))s"; if ($_.failure) { $_.failure.message.'#cdata-section' } }
} else { "No results (exit $($p.ExitCode)). Is the project open in Unity? Close it first."; Select-String -Path $log -Pattern 'error CS|Exception' | Select-Object -First 20 | ForEach-Object { $_.Line } }
