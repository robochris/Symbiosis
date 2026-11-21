# Which Unity the scripts run: the version the project is on (ProjectSettings\ProjectVersion.txt), from Unity Hub's
# usual install folder. Set $env:SHRUNK_UNITY to a Unity.exe to use a different one (for an upgrade, say).
$version = (Select-String -Path (Join-Path $PSScriptRoot 'ProjectSettings\ProjectVersion.txt') -Pattern '^m_EditorVersion:\s*(\S+)').Matches[0].Groups[1].Value
$unity = if ($env:SHRUNK_UNITY) { $env:SHRUNK_UNITY } else { "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe" }
if (-not (Test-Path $unity)) { throw "Unity $version is not installed at $unity" }
