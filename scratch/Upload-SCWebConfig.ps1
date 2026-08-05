param(
    [string]$ftpHost = "WIN8236.site4now.net",
    [string]$user = "tspmasterprd",
    [string]$pass = "ftp@dm1n1str@t0r",
    [string]$localFile = "C:\GitHub\TSPMaster\publish_sc\web.config",
    [string]$remoteUri = "ftp://WIN8236.site4now.net/api/web.config"
)

Write-Host "Uploading: $localFile -> $remoteUri"
$req = [System.Net.FtpWebRequest]::Create($remoteUri)
$req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
$req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
$req.UseBinary = $true
$req.UsePassive = $true
$req.KeepAlive = $false
$bytes = [System.IO.File]::ReadAllBytes($localFile)
$req.ContentLength = $bytes.Length
$strm = $req.GetRequestStream()
$strm.Write($bytes, 0, $bytes.Length)
$strm.Close()
$resp = $req.GetResponse()
$resp.Close()
Write-Host "Uploaded successfully."

Write-Host "Waiting 10s for IIS to recycle..."
Start-Sleep -Seconds 10

Write-Host "Testing http://api.tspmaster.com/health ..."
try {
    $r = Invoke-WebRequest -Uri "http://api.tspmaster.com/health" -TimeoutSec 25 -UseBasicParsing -ErrorAction Stop
    Write-Host "SUCCESS! Status: $($r.StatusCode)"
    Write-Host "Body: $($r.Content)"
} catch [System.Net.WebException] {
    $code = [int]$_.Exception.Response.StatusCode
    Write-Host "HTTP $code - $($_.Exception.Message)"
} catch {
    Write-Host "Error: $($_.GetType().Name) - $($_.Exception.Message)"
}
