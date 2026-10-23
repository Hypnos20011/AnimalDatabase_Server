param(
    [string]$Server = 'deploy@49.12.226.169',
    [string]$RemoteDir = '/opt/animaldatabase',
    [string]$ApiRepo = (Split-Path $PSScriptRoot -Parent),
    [string]$UiRepo = 'D:\AnimalDatabaseProject',
    [string]$Ref = 'master',
    [string]$Schema,
    [switch]$SkipBackup
)

$ErrorActionPreference = 'Stop'

function Invoke-Native([string]$File, [string[]]$Arguments) {
    $ErrorActionPreference = 'Continue'
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$File failed with exit code $LASTEXITCODE" }
}

$work = Join-Path $env:TEMP ('adb-redeploy-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $work | Out-Null
try {
    $apiTar = Join-Path $work 'server.tar.gz'
    $uiTar = Join-Path $work 'ui.tar.gz'
    Invoke-Native git @('-c', "safe.directory=$($ApiRepo -replace '\\', '/')", '-C', $ApiRepo, 'archive', '--format=tar.gz', '-o', $apiTar, $Ref)
    Invoke-Native git @('-c', "safe.directory=$($UiRepo -replace '\\', '/')", '-C', $UiRepo, 'archive', '--format=tar.gz', '-o', $uiTar, $Ref)

    $uploads = @($apiTar, $uiTar)
    if ($Schema) {
        $schemaCopy = Join-Path $work 'schema.sql'
        Copy-Item $Schema $schemaCopy
        $uploads += $schemaCopy
    }
    Invoke-Native scp (@('-q', '-o', 'BatchMode=yes') + $uploads + "${Server}:/tmp/")

    $backup = if ($SkipBackup) { '0' } else { '1' }
    $schemaFlag = if ($Schema) { '1' } else { '0' }
    $remote = @"
set -euo pipefail
cd '$RemoteDir'
test -f AnimalDatabase_Server/deploy/.env || { echo 'Missing AnimalDatabase_Server/deploy/.env on the server' >&2; exit 1; }
if [ '$backup' = '1' ] && docker compose -f AnimalDatabase_Server/deploy/docker-compose.yml ps --status running --services 2>/dev/null | grep -qx backup; then
  (cd AnimalDatabase_Server/deploy && docker compose exec -T backup bash /usr/local/bin/backup.sh now)
fi
rm -rf AnimalDatabase_Server.new AnimalDatabase_UI.new
mkdir AnimalDatabase_Server.new AnimalDatabase_UI.new
tar -xzf /tmp/server.tar.gz -C AnimalDatabase_Server.new
tar -xzf /tmp/ui.tar.gz -C AnimalDatabase_UI.new
cp -p AnimalDatabase_Server/deploy/.env AnimalDatabase_Server.new/deploy/.env
if [ '$schemaFlag' = '1' ]; then cp /tmp/schema.sql AnimalDatabase_Server.new/deploy/schema.sql; fi
rm -rf AnimalDatabase_Server.old AnimalDatabase_UI.old
mv AnimalDatabase_Server AnimalDatabase_Server.old
if [ -d AnimalDatabase_UI ]; then mv AnimalDatabase_UI AnimalDatabase_UI.old; fi
mv AnimalDatabase_Server.new AnimalDatabase_Server
mv AnimalDatabase_UI.new AnimalDatabase_UI
rm -f /tmp/server.tar.gz /tmp/ui.tar.gz /tmp/schema.sql
cd AnimalDatabase_Server/deploy
docker compose up -d --build
docker image prune -f >/dev/null
docker compose ps
"@
    $remoteScript = Join-Path $work 'redeploy.sh'
    [IO.File]::WriteAllText($remoteScript, ($remote -replace "`r`n", "`n"))
    Invoke-Native scp @('-q', '-o', 'BatchMode=yes', $remoteScript, "${Server}:/tmp/adb-redeploy.sh")
    Invoke-Native ssh @('-o', 'BatchMode=yes', $Server, 'bash /tmp/adb-redeploy.sh; rc=$?; rm -f /tmp/adb-redeploy.sh; exit $rc')
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}