param(
    [string]$ftpHost = "WIN8236.site4now.net",
    [string]$user = "tspmasterprd",
    [string]$pass = "ftp@dm1n1str@t0r"
)

function FtpUploadString($content, $remoteUri) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
    $req = [System.Net.FtpWebRequest]::Create($remoteUri)
    $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $req.UseBinary = $true; $req.UsePassive = $true; $req.KeepAlive = $false
    $req.ContentLength = $bytes.Length
    $s = $req.GetRequestStream(); $s.Write($bytes,0,$bytes.Length); $s.Close()
    $resp = $req.GetResponse(); $resp.Close()
    Write-Host "Uploaded to $remoteUri"
}

# Absolute minimal web.config - no handlers, no mime, just bare bones
$minConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <directoryBrowse enabled="false" />
  </system.webServer>
</configuration>
"@

FtpUploadString $minConfig "ftp://$ftpHost/api/web.config"

Write-Host "Waiting 5s..."
Start-Sleep -Seconds 5

# Test plain text file
Write-Host "Testing http://api.tspmaster.com/test.txt ..."
try {
    $r = Invoke-WebRequest -Uri "http://api.tspmaster.com/test.txt" -TimeoutSec 15 -UseBasicParsing -ErrorAction Stop
    Write-Host "SUCCESS Status=$($r.StatusCode) - Subdomain IS mapped to /api/ folder"
    Write-Host "Content: $($r.Content)"
} catch [System.Net.WebException] {
    $code = [int]$_.Exception.Response.StatusCode
    Write-Host "HTTP $code response"
    if ($code -eq 404) { Write-Host "404 = file not found BUT subdomain is reaching IIS correctly" }
    if ($code -eq 403) { Write-Host "403 = directory listing forbidden BUT subdomain IS working" }
    if ($code -eq 500) { Write-Host "500 = IIS config error in web.config" }
} catch {
    Write-Host "Network error: $($_.Exception.Message)"
}

# Also try root path
Write-Host "`nTesting http://api.tspmaster.com/ ..."
try {
    $r = Invoke-WebRequest -Uri "http://api.tspmaster.com/" -TimeoutSec 15 -UseBasicParsing -ErrorAction Stop
    Write-Host "Root returned Status=$($r.StatusCode)"
} catch [System.Net.WebException] {
    $code = [int]$_.Exception.Response.StatusCode
    Write-Host "Root returned HTTP $code"
} catch {
    Write-Host "Root error: $($_.Exception.Message)"
}
