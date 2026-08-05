$ftpHost = "ftp://WIN8236.site4now.net/"
$user = "tspmasterprd"
$pass = "tspmasterprd"

[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12 -bor [System.Net.SecurityProtocolType]::Tls13

$req = [System.Net.WebRequest]::Create($ftpHost)
$req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
$req.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
$req.EnableSsl = $true
$req.UsePassive = $true
$req.KeepAlive = $false

try {
    $resp = $req.GetResponse()
    $stream = $resp.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    $content = $reader.ReadToEnd()
    Write-Host "=================> SUCCESS via PowerShell FtpWebRequest!"
    Write-Host $content
    $reader.Close()
    $resp.Close()
} catch {
    Write-Host "PowerShell FTPS Error: $_"
    if ($_.Exception.Response) {
        $status = $_.Exception.Response.StatusCode
        Write-Host "FTP Status Code: $status"
    }
}
