param(
    [string]$ftpHost = "WIN8236.site4now.net",
    [string]$user = "tspmasterprd",
    [string]$pass = "ftp@dm1n1str@t0r"
)

$uri = "ftp://$ftpHost/api/logs"
Write-Host "Creating FTP directory: $uri"

try {
    $req = [System.Net.FtpWebRequest]::Create($uri)
    $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
    $req.UseBinary = $true
    $req.KeepAlive = $false
    $resp = $req.GetResponse()
    $resp.Close()
    Write-Host "SUCCESS: Created /api/logs directory on server."
} catch {
    Write-Host "Note: $($_.Exception.Message) (directory may already exist or IIS created it)"
}
