param(
    [string]$Server = 'deploy@49.12.226.169',
    [string]$RemoteDir = '/opt/animaldatabase/backups',
    [string]$LocalDir = 'D:\AnimalDatabase-server-backups',
    [int]$Keep = 60
)

$ErrorActionPreference = 'Stop'
$sshOptions = @('-o', 'BatchMode=yes', '-o', 'StrictHostKeyChecking=yes', '-o', 'ConnectTimeout=20')
$logFile = Join-Path $LocalDir 'pull.log'

function Write-Log([string]$Message) {
    $line = '{0} {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Message
    Add-Content -Path $logFile -Value $line -Encoding UTF8
    Write-Output $line
}

function Invoke-Ssh([string]$Command) {
    $ErrorActionPreference = 'Continue'
    $output = & ssh @sshOptions $Server $Command 2>$null
    if ($LASTEXITCODE -ne 0) { throw "ssh exited with code $LASTEXITCODE" }
    $output
}

$drive = Split-Path -Qualifier $LocalDir
if (-not (Test-Path "$drive\")) {
    Write-Output "Drive $drive is not connected; nothing to do."
    exit 0
}
if (-not (Test-Path -PathType Container $LocalDir)) {
    Write-Output "Folder $LocalDir does not exist; nothing to do."
    exit 0
}

try {
    $listing = Invoke-Ssh "find '$RemoteDir' -maxdepth 1 -type f -name '*.sql.gz' -printf '%f %s\n'"
}
catch {
    Write-Log "server unreachable ($($_.Exception.Message)); nothing copied"
    exit 0
}

$remoteFiles = @{}
foreach ($entry in @($listing)) {
    if ($entry -match '^([A-Za-z0-9._-]+\.sql\.gz) (\d+)$') { $remoteFiles[$Matches[1]] = [long]$Matches[2] }
}

$missing = @($remoteFiles.Keys | Where-Object { -not (Test-Path -LiteralPath (Join-Path $LocalDir $_)) } | Sort-Object)
$copied = 0
$failed = @()

if ($missing.Count -gt 0) {
    try {
        $hashes = @{}
        $quoted = ($missing | ForEach-Object { "'$RemoteDir/$_'" }) -join ' '
        foreach ($entry in @(Invoke-Ssh "sha256sum $quoted")) {
            if ($entry -match '^([0-9a-f]{64})\s+\*?(.+)$') { $hashes[(Split-Path -Leaf $Matches[2])] = $Matches[1] }
        }
        foreach ($name in $missing) {
            $target = Join-Path $LocalDir $name
            $partial = "$target.partial"
            Remove-Item -LiteralPath $partial -ErrorAction SilentlyContinue
            $ErrorActionPreference = 'Continue'
            & scp -q @sshOptions "${Server}:$RemoteDir/$name" $partial 2>$null
            $scpExit = $LASTEXITCODE
            $ErrorActionPreference = 'Stop'
            $ok = $scpExit -eq 0 -and (Test-Path -LiteralPath $partial)
            if ($ok) {
                $size = (Get-Item -LiteralPath $partial).Length
                $hash = (Get-FileHash -LiteralPath $partial -Algorithm SHA256).Hash.ToLowerInvariant()
                $ok = $size -eq $remoteFiles[$name] -and $hashes.ContainsKey($name) -and $hash -eq $hashes[$name]
            }
            if ($ok) {
                Move-Item -LiteralPath $partial -Destination $target
                $copied++
            }
            else {
                Remove-Item -LiteralPath $partial -ErrorAction SilentlyContinue
                $failed += $name
            }
        }
    }
    catch {
        Write-Log "error during copy ($($_.Exception.Message)); copied $copied, failed $($missing.Count - $copied)"
        exit 1
    }
}

$local = @(Get-ChildItem -LiteralPath $LocalDir -Filter '*.sql.gz' -File | Sort-Object Name -Descending)
$removed = 0
if ($local.Count -gt $Keep) {
    foreach ($old in $local[$Keep..($local.Count - 1)]) {
        Remove-Item -LiteralPath $old.FullName
        $removed++
    }
}
$total = @(Get-ChildItem -LiteralPath $LocalDir -Filter '*.sql.gz' -File).Count

$summary = "server $($remoteFiles.Count), copied $copied (SHA-256 verified), removed locally $removed, local total $total"
if ($failed.Count -gt 0) {
    Write-Log "$summary; FAILED: $($failed -join ', ')"
    exit 1
}
Write-Log $summary