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

function FtpUploadFile($localPath, $remoteUri) {
    $bytes = [System.IO.File]::ReadAllBytes($localPath)
    $req = [System.Net.FtpWebRequest]::Create($remoteUri)
    $req.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $req.UseBinary = $true; $req.UsePassive = $true; $req.KeepAlive = $false
    $req.ContentLength = $bytes.Length
    $s = $req.GetRequestStream(); $s.Write($bytes,0,$bytes.Length); $s.Close()
    $resp = $req.GetResponse(); $resp.Close()
    Write-Host "Uploaded: $localPath"
}

# Restore the framework-dependent web.config
# Uses system dotnet.exe (trusted, not blocked by AV) with outofprocess mode
$webConfig = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\TSPMaster.API.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="outofprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
          <environmentVariable name="ASPNETCORE_FORWARDEDHEADERS_ENABLED" value="true" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
<!--ProjectGuid: C625A8CB-8BCE-E22D-EF62-26313DDF0DDC-->
'@

FtpUploadString $webConfig "ftp://$ftpHost/api/web.config"

# Also restore the correct TSPMaster.API.dll from framework-dependent build
FtpUploadFile "C:\GitHub\TSPMaster\publish\TSPMaster.API.dll" "ftp://$ftpHost/api/TSPMaster.API.dll"
FtpUploadFile "C:\GitHub\TSPMaster\publish\TSPMaster.API.deps.json" "ftp://$ftpHost/api/TSPMaster.API.deps.json"
FtpUploadFile "C:\GitHub\TSPMaster\publish\TSPMaster.API.runtimeconfig.json" "ftp://$ftpHost/api/TSPMaster.API.runtimeconfig.json"

Write-Host ""
Write-Host "=== web.config restored with framework-dependent outofprocess config ==="
Write-Host "=== ACTION REQUIRED: Please go to site4now control panel and ==="
Write-Host "=== restart/recycle the application pool for api.tspmaster.com ==="
