param(
    [string]$ftpHost = "WIN8236.site4now.net",
    [string]$user = "tspmasterprd",
    [string]$pass = "ftp@dm1n1str@t0r"
)

function Upload-File($localPath, $remoteUri) {
    $req = [System.Net.FtpWebRequest]::Create($remoteUri)
    $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $req.UseBinary = $true
    $req.UsePassive = $true
    $req.KeepAlive = $false
    $bytes = [System.IO.File]::ReadAllBytes($localPath)
    $req.ContentLength = $bytes.Length
    $strm = $req.GetRequestStream()
    $strm.Write($bytes, 0, $bytes.Length)
    $strm.Close()
    $resp = $req.GetResponse()
    $resp.Close()
    Write-Host "Uploaded: $localPath -> $remoteUri"
}

# Upload just the web.config
Upload-File "C:\GitHub\TSPMaster\publish\web.config" "ftp://$ftpHost/api/web.config"

Write-Host "Done. Waiting 5s for IIS to recycle app pool..."
Start-Sleep -Seconds 5

# Test the health endpoint
Write-Host "Testing health endpoint..."
try {
    $resp = Invoke-WebRequest -Uri "http://api.tspmaster.com/health" -TimeoutSec 15 -UseBasicParsing
    Write-Host "SUCCESS! Status: $($resp.StatusCode)"
    Write-Host "Response: $($resp.Content)"
} catch {
    Write-Host "Still failing: $($_.Exception.Response.StatusCode) - $($_.Exception.Message)"
}
