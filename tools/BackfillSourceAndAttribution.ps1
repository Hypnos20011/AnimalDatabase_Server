# Backfill animal source_url + image attribution/source_page_url (batched Wikimedia APIs).
param(
  [switch]$ApplyLocal,
  [string]$SqlOut = "D:\AnimalDatabase.Api\tools\backfill-source-attribution.sql",
  [string]$Mysql = "C:\Program Files\MariaDB 11.8\bin\mysql.exe",
  [string]$ReportOut = "D:\AnimalDatabase.Api\tools\backfill-report.json"
)
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$UA = "AnimalDatabaseBackfill/1.2"

function Escape-Sql([string]$s) { if ($null -eq $s) { return $null }; return $s.Replace("\", "\\").Replace("'", "''") }
function Truncate([string]$s, [int]$max) {
  if ($null -eq $s) { return $null }
  $t = ($s -replace "\s+", " ").Trim()
  if ($t.Length -eq 0) { return $null }
  if ($t.Length -le $max) { return $t }
  return $t.Substring(0, $max)
}
function Strip-Html([string]$s) {
  if ($null -eq $s) { return $null }
  return Truncate ([System.Net.WebUtility]::HtmlDecode(($s -replace "<[^>]+>", " "))) 500
}
function Is-TaxonName([string]$name) {
  return $name -match "^[A-Za-z][A-Za-z\-]*(?:\s+[a-z][a-z\-]*){0,3}$"
}
function FileName-FromUrl([string]$url) {
  $m = [regex]::Match($url, "/(?:thumb/)?[0-9a-f]/[0-9a-f]{2}/([^/?]+)")
  if (-not $m.Success) { return $null }
  return [Uri]::UnescapeDataString($m.Groups[1].Value)
}
function Invoke-WikiJson([string]$url, [int]$maxAttempts = 8) {
  $delay = 2
  for ($i = 1; $i -le $maxAttempts; $i++) {
    try {
      $resp = Invoke-WebRequest -Uri $url -UserAgent $UA -TimeoutSec 60 -UseBasicParsing
      if ($resp.StatusCode -eq 200) {
        return ($resp.Content | ConvertFrom-Json)
      }
      Write-Warning "HTTP $($resp.StatusCode) for $url"
    } catch {
      $status = $null
      if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
      if ($status -eq 429 -or ($status -ge 500 -and $status -lt 600) -or $null -eq $status) {
        Write-Warning "HTTP $status attempt $i; sleeping ${delay}s ($($_.Exception.Message))"
        Start-Sleep -Seconds $delay
        $delay = [Math]::Min(60, $delay * 2)
        continue
      }
      Write-Warning "HTTP $status for $url :: $($_.Exception.Message)"
      return $null
    }
  }
  return $null
}
function Resolve-AnimalUrls([string[]]$names) {
  $map = @{}
  $chunkSize = 40
  for ($i = 0; $i -lt $names.Count; $i += $chunkSize) {
    $chunk = $names[$i..([Math]::Min($i+$chunkSize-1, $names.Count-1))]
    $titles = ($chunk | ForEach-Object { $_ -replace " ", "_" }) -join "|"
    $url = "https://en.wikipedia.org/w/api.php?action=query&titles=$([Uri]::EscapeDataString($titles))&prop=info|pageprops&inprop=url&ppprop=disambiguation&redirects=1&format=json&formatversion=2"
    Write-Host "Wikipedia batch $($i+1)-$($i+$chunk.Count)..."
    $doc = Invoke-WikiJson $url
    Start-Sleep -Seconds 1
    if (-not $doc -or -not $doc.query) { continue }

    # Build redirect/normalized maps
    $norm = @{}
    if ($doc.query.normalized) {
      foreach ($n in @($doc.query.normalized)) { $norm[$n.from] = $n.to }
    }
    $redir = @{}
    if ($doc.query.redirects) {
      foreach ($r in @($doc.query.redirects)) { $redir[$r.from] = $r.to }
    }
    $pagesByTitle = @{}
    foreach ($p in @($doc.query.pages)) {
      if ($p.title) { $pagesByTitle[$p.title] = $p }
    }

    foreach ($name in $chunk) {
      $key = $name -replace " ", "_"
      $cur = $key
      if ($norm.ContainsKey($cur)) { $cur = $norm[$cur] }
      # also try spaced form as normalized from
      if ($norm.ContainsKey($name)) { $cur = $norm[$name] }
      $guard = 0
      while ($redir.ContainsKey($cur) -and $guard -lt 5) { $cur = $redir[$cur]; $guard++ }
      $page = $null
      if ($pagesByTitle.ContainsKey($cur)) { $page = $pagesByTitle[$cur] }
      elseif ($pagesByTitle.ContainsKey($name)) { $page = $pagesByTitle[$name] }
      if (-not $page -or $page.missing) { continue }
      if ($page.pageprops -and $page.pageprops.PSObject.Properties.Name -contains "disambiguation") { continue }
      $canon = $null
      if ($page.canonicalurl) { $canon = [string]$page.canonicalurl }
      elseif ($page.fullurl) { $canon = ([string]$page.fullurl) -replace "^http://","https://" }
      if ($canon -and $canon -match "^https://en\.wikipedia\.org/wiki/") {
        $map[$name] = $canon
      }
    }
  }
  return $map
}

function Resolve-ImageMetas([string[]]$files) {
  $map = @{}
  $chunkSize = 40
  for ($i = 0; $i -lt $files.Count; $i += $chunkSize) {
    $chunk = $files[$i..([Math]::Min($i+$chunkSize-1, $files.Count-1))]
    $titles = ($chunk | ForEach-Object { "File:$_" }) -join "|"
    $url = "https://commons.wikimedia.org/w/api.php?action=query&titles=$([Uri]::EscapeDataString($titles))&prop=imageinfo&iiprop=extmetadata|url&iiextmetadatafilter=Artist&iiextmetadatalanguage=en&format=json&formatversion=2"
    Write-Host "Commons batch $($i+1)-$($i+$chunk.Count)..."
    $doc = Invoke-WikiJson $url
    Start-Sleep -Seconds 1
    if (-not $doc -or -not $doc.query -or -not $doc.query.pages) { continue }
    foreach ($p in @($doc.query.pages)) {
      if ($p.missing -or -not $p.imageinfo) { continue }
      $ii = @($p.imageinfo)[0]
      $fileTitle = [string]$p.title
      $fileKey = ($fileTitle -replace "^(File|Image):","")
      $artist = $null
      if ($ii.extmetadata -and $ii.extmetadata.Artist -and $ii.extmetadata.Artist.value) {
        $artist = Strip-Html ([string]$ii.extmetadata.Artist.value)
      }
      $pageUrl = $null
      if ($ii.descriptionurl) {
        $pageUrl = Truncate (([string]$ii.descriptionurl).Split("?")[0]) 500
        if ($pageUrl -notmatch "^https://") { $pageUrl = $null }
      }
      if (-not $artist -and -not $pageUrl) { continue }
      $map[$fileKey] = @{ Attribution = $artist; SourcePageUrl = $pageUrl }
      # also map URL-decoded / underscore variants
      $map[($fileKey -replace " ","_")] = $map[$fileKey]
    }
  }
  return $map
}

# Load remaining rows
$animalsRaw = & $Mysql -uroot -proot animaldatabase --default-character-set=utf8mb4 -N -B -e "SELECT id, common_name, IFNULL(scientific_name,'') FROM animals WHERE source_url IS NULL OR source_url='' ORDER BY id;"
$imagesRaw = & $Mysql -uroot -proot animaldatabase --default-character-set=utf8mb4 -N -B -e "SELECT id, url, IFNULL(attribution,''), IFNULL(source_page_url,'') FROM animal_images WHERE (attribution IS NULL OR attribution='') OR (source_page_url IS NULL OR source_page_url='') ORDER BY id;"

$animals = @()
foreach ($line in ($animalsRaw -split "`n" | Where-Object { $_.Trim() })) {
  $c = $line -split "`t", 3
  if ($c.Count -lt 3) { continue }
  $animals += [pscustomobject]@{ Id=[int]$c[0]; Common=$c[1]; Sci=$c[2].Trim() }
}
$images = @()
foreach ($line in ($imagesRaw -split "`n" | Where-Object { $_.Trim() })) {
  $c = $line -split "`t", 4
  if ($c.Count -lt 2) { continue }
  $images += [pscustomobject]@{
    Id=[int]$c[0]; Url=$c[1]
    CurAttr= if ($c.Count -gt 2) { $c[2] } else { "" }
    CurPage= if ($c.Count -gt 3) { $c[3] } else { "" }
  }
}

Write-Host "Remaining animals: $($animals.Count); images: $($images.Count)"

$taxonNames = @($animals | Where-Object { Is-TaxonName $_.Sci } | ForEach-Object { $_.Sci } | Select-Object -Unique)
$invalid = @($animals | Where-Object { -not (Is-TaxonName $_.Sci) })
Write-Host "Valid taxon names to resolve: $($taxonNames.Count); invalid sci names: $($invalid.Count)"

$urlMap = Resolve-AnimalUrls $taxonNames

$files = @()
$fileByImageId = @{}
foreach ($img in $images) {
  $fn = FileName-FromUrl $img.Url
  if ($fn) {
    $files += $fn
    $fileByImageId[$img.Id] = $fn
  }
}
$files = @($files | Select-Object -Unique)
$metaMap = Resolve-ImageMetas $files

$animalUpdates = @(); $animalSkipped = @()
$imageUpdates = @(); $imageSkipped = @()
$sql = New-Object System.Collections.Generic.List[string]
$sql.Add("-- Backfill generated $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') Europe/Vienna")
$sql.Add("SET NAMES utf8mb4;")
$sql.Add("START TRANSACTION;")

foreach ($a in $animals) {
  if (-not (Is-TaxonName $a.Sci)) {
    $animalSkipped += [pscustomobject]@{ Id=$a.Id; CommonName=$a.Common; ScientificName=$a.Sci; Reason="invalid scientific_name" }
    continue
  }
  if ($urlMap.ContainsKey($a.Sci)) {
    $u = $urlMap[$a.Sci]
    $animalUpdates += [pscustomobject]@{ Id=$a.Id; CommonName=$a.Common; ScientificName=$a.Sci; SourceUrl=$u }
    $sql.Add("UPDATE animals SET source_url='$(Escape-Sql $u)' WHERE id=$($a.Id) AND (source_url IS NULL OR source_url='');")
  } else {
    $animalSkipped += [pscustomobject]@{ Id=$a.Id; CommonName=$a.Common; ScientificName=$a.Sci; Reason="no EN Wikipedia page for scientific_name" }
  }
}

foreach ($img in $images) {
  if (-not $fileByImageId.ContainsKey($img.Id)) {
    $imageSkipped += [pscustomobject]@{ Id=$img.Id; Url=$img.Url; Reason="not wikimedia/unparseable" }
    continue
  }
  $fn = $fileByImageId[$img.Id]
  $meta = $null
  if ($metaMap.ContainsKey($fn)) { $meta = $metaMap[$fn] }
  elseif ($metaMap.ContainsKey(($fn -replace "_"," "))) { $meta = $metaMap[($fn -replace "_"," ")] }
  else {
    # try case-insensitive
    $hit = $metaMap.Keys | Where-Object { $_ -ieq $fn -or $_ -ieq ($fn -replace "_"," ") } | Select-Object -First 1
    if ($hit) { $meta = $metaMap[$hit] }
  }
  if (-not $meta) {
    $imageSkipped += [pscustomobject]@{ Id=$img.Id; Url=$img.Url; Reason="commons lookup failed/missing" }
    continue
  }
  $sets = @(); $newAttr=$null; $newPage=$null
  if ([string]::IsNullOrWhiteSpace($img.CurAttr) -and $meta.Attribution) {
    $newAttr = $meta.Attribution
    $sets += "attribution='$(Escape-Sql $newAttr)'"
  }
  if ([string]::IsNullOrWhiteSpace($img.CurPage) -and $meta.SourcePageUrl) {
    $newPage = $meta.SourcePageUrl
    $sets += "source_page_url='$(Escape-Sql $newPage)'"
  }
  if ($sets.Count -eq 0) {
    $imageSkipped += [pscustomobject]@{ Id=$img.Id; Url=$img.Url; Reason="no fillable fields from meta" }
    continue
  }
  $imageUpdates += [pscustomobject]@{ Id=$img.Id; Attribution=$newAttr; SourcePageUrl=$newPage; Url=$img.Url }
  $sql.Add(("UPDATE animal_images SET {0} WHERE id={1};" -f ($sets -join ", "), $img.Id))
}

$sql.Add("COMMIT;")
[System.IO.File]::WriteAllText($SqlOut, (($sql -join "`r`n") + "`r`n"), [System.Text.UTF8Encoding]::new($false))

# Merge with already-filled counts for full report
$alreadyAnimals = [int]((& $Mysql -uroot -proot animaldatabase --default-character-set=utf8mb4 -N -B -e "SELECT COUNT(*) FROM animals WHERE source_url IS NOT NULL AND source_url<>'';").Trim())
$alreadyAttr = [int]((& $Mysql -uroot -proot animaldatabase --default-character-set=utf8mb4 -N -B -e "SELECT COUNT(*) FROM animal_images WHERE attribution IS NOT NULL AND attribution<>'';").Trim())

$report = [pscustomobject]@{
  GeneratedAt = (Get-Date).ToString("o")
  ThisRunAnimalsUpdated = $animalUpdates.Count
  ThisRunAnimalsSkipped = $animalSkipped.Count
  ThisRunImagesUpdated = $imageUpdates.Count
  ThisRunImagesSkipped = $imageSkipped.Count
  AnimalsAlreadyHadSourceBeforeApply = $alreadyAnimals
  ImagesAlreadyHadAttrBeforeApply = $alreadyAttr
  Animals = $animalUpdates
  AnimalSkips = $animalSkipped
  Images = $imageUpdates
  ImageSkips = $imageSkipped
  SqlOut = $SqlOut
}
$report | ConvertTo-Json -Depth 6 | Set-Content -Path $ReportOut -Encoding UTF8
Write-Host "`nThis run: animals +$($animalUpdates.Count) skip $($animalSkipped.Count); images +$($imageUpdates.Count) skip $($imageSkipped.Count)"
if ($animalSkipped.Count) { $animalSkipped | Format-Table Id,CommonName,ScientificName,Reason -AutoSize }
if ($imageSkipped.Count) { $imageSkipped | Format-Table Id,Reason -AutoSize }

if ($ApplyLocal) {
  Write-Host "Applying locally..."
  & $Mysql -uroot -proot animaldatabase --default-character-set=utf8mb4 -e "SOURCE $($SqlOut -replace '\\','/')"
  if ($LASTEXITCODE -ne 0) { throw "Local apply failed" }
  Write-Host "Local apply OK"
  & $Mysql -uroot -proot animaldatabase --default-character-set=utf8mb4 -e "SELECT COUNT(*) AS animals_with_source FROM animals WHERE source_url IS NOT NULL AND source_url<>''; SELECT COUNT(*) AS animals_missing_source FROM animals WHERE source_url IS NULL OR source_url=''; SELECT COUNT(*) AS images_with_attr FROM animal_images WHERE attribution IS NOT NULL AND attribution<>''; SELECT COUNT(*) AS images_with_page FROM animal_images WHERE source_page_url IS NOT NULL AND source_page_url<>''; SELECT COUNT(*) AS images_missing_either FROM animal_images WHERE attribution IS NULL OR attribution='' OR source_page_url IS NULL OR source_page_url='';"
}
