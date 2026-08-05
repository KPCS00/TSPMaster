param(
    [string]$ftpHost = "WIN8236.site4now.net",
    [string]$user = "tspmasterprd",
    [string]$pass = "ftp@dm1n1str@t0r"
)

$localOut = "C:\GitHub\TSPMaster\scratch\server-logs"
New-Item -ItemType Directory -Force -Path $localOut | Out-Null

function List-FtpDir($path) {
    $uri = "ftp://$ftpHost$path"
    $req = [System.Net.FtpWebRequest]::Create($uri)
    $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
    $req.UseBinary = $true
    $req.UsePassive = $true
    $req.KeepAlive = $false
    try {
        $resp = $req.GetResponse()
        $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
        $content = $reader.ReadToEnd() -split "`r`n" | Where-Object { $_ -ne "" }
        $reader.Close(); $resp.Close()
        return $content
    } catch { return @() }
}

function Download-File($remotePath, $localPath) {
    $uri = "ftp://$ftpHost$remotePath"
    $req = [System.Net.FtpWebRequest]::Create($uri)
    $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::DownloadFile
    $req.UseBinary = $true
    $req.UsePassive = $true
    $req.KeepAlive = $false
    try {
        $resp = $req.GetResponse()
        $stream = $resp.GetResponseStream()
        $out = [System.IO.File]::Create($localPath)
        $stream.CopyTo($out)
        $out.Close(); $stream.Close(); $resp.Close()
        Write-Host "Downloaded: $remotePath -> $localPath"
        Get-Content $localPath | Select-Object -Last 50
    } catch {
        Write-Warning "Cannot download $remotePath : $($_.Exception.Message)"
    }
}

# Check /api/logs/
Write-Host "=== /api/logs/ ==="
$logs = List-FtpDir "/api/logs/"
if ($logs.Count -eq 0) { Write-Host "(empty)" } else { $logs | ForEach-Object { Write-Host $_ } }
foreach ($f in $logs) {
    Download-File "/api/logs/$f" (Join-Path $localOut $f)
}

# Check /api/ root for any .log files
Write-Host "`n=== /api/ root files ==="
$apiFiles = List-FtpDir "/api/"
$apiFiles | Where-Object { $_ -match '\.(log|txt)$' } | ForEach-Object { Write-Host $_ }

# Check if there's an App_Data or similar
Write-Host "`n=== Done ==="
