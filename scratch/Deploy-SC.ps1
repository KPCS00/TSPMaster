param(
    [string]$ftpHost = "WIN8236.site4now.net",
    [string]$user = "tspmasterprd",
    [string]$pass = "ftp@dm1n1str@t0r"
)

function Create-FtpDirectory {
    param([string]$remoteUri)
    try {
        $req = [System.Net.FtpWebRequest]::Create($remoteUri)
        $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $req.UseBinary = $true
        $req.UsePassive = $true
        $req.KeepAlive = $false
        $resp = $req.GetResponse()
        $resp.Close()
    } catch { }
}

function Upload-Folder {
    param([string]$LocalFolder, [string]$RemoteBaseUri)
    $files = Get-ChildItem -Path $LocalFolder -Recurse
    foreach ($file in $files) {
        $relPath = $file.FullName.Substring($LocalFolder.Length).TrimStart('\').Replace('\', '/')
        $targetUri = "$RemoteBaseUri/$relPath"

        if ($file.PSIsContainer) {
            Create-FtpDirectory -remoteUri $targetUri
        } else {
            $parentRel = [System.IO.Path]::GetDirectoryName($file.FullName.Substring($LocalFolder.Length)).TrimStart('\').Replace('\', '/')
            if ($parentRel) {
                $parentUri = "$RemoteBaseUri/$parentRel"
                Create-FtpDirectory -remoteUri $parentUri
            }
            try {
                $req = [System.Net.FtpWebRequest]::Create($targetUri)
                $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
                $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
                $req.UseBinary = $true
                $req.UsePassive = $true
                $req.KeepAlive = $false
                $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
                $req.ContentLength = $bytes.Length
                $strm = $req.GetRequestStream()
                $strm.Write($bytes, 0, $bytes.Length)
                $strm.Close()
                $resp = $req.GetResponse()
                $resp.Close()
                Write-Host "Uploaded: $relPath"
            } catch {
                Write-Warning "Failed: $relPath - $($_.Exception.Message)"
            }
        }
    }
}

Write-Host "=== Deploying SELF-CONTAINED API to $ftpHost/api ==="
Create-FtpDirectory -remoteUri "ftp://$ftpHost/api"
Create-FtpDirectory -remoteUri "ftp://$ftpHost/api/logs"
Upload-Folder -LocalFolder "C:\GitHub\TSPMaster\publish_sc" -RemoteBaseUri "ftp://$ftpHost/api"
Write-Host "=== Self-Contained Deployment Completed ==="
