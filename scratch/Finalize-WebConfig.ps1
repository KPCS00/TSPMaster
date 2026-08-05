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

# Final production web.config — stdout logging OFF, proper aspNetCore outofprocess
$webConfig = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\TSPMaster.API.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="outofprocess">
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
Write-Host "Done. stdout logging disabled for production."
