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
        $req.KeepAlive = $false
        $resp = $req.GetResponse()
        $resp.Close()
    } catch {
        # Directory might already exist
    }
}

function Upload-Folder {
    param(
        [string]$LocalFolder,
        [string]$RemoteBaseUri
    )

    $files = Get-ChildItem -Path $LocalFolder -Recurse
    foreach ($file in $files) {
        $relPath = $file.FullName.Substring($LocalFolder.Length).TrimStart('\').Replace('\', '/')
        $targetUri = "$RemoteBaseUri/$relPath"

        if ($file.PSIsContainer) {
            Create-FtpDirectory -remoteUri $targetUri
        } else {
            # Ensure parent remote folder exists
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
                Write-Warning "Failed to upload $relPath"
            }
        }
    }
}

Write-Host "=== Deploying API to $ftpHost/api ==="
Create-FtpDirectory -remoteUri "ftp://$ftpHost/api"
Upload-Folder -LocalFolder "c:\GitHub\TSPMaster\publish" -RemoteBaseUri "ftp://$ftpHost/api"

Write-Host "=== Deploying Client to $ftpHost/ ==="
Upload-Folder -LocalFolder "c:\GitHub\TSPMaster\publish_client\wwwroot" -RemoteBaseUri "ftp://$ftpHost"

Write-Host "=== FTP Deployment Completed Successfully ==="
