# =============================================================================
# TSP Historical Fund Price Seeder (Wide Table Schema)
# Downloads the full TSP share price history from tsp.gov and bulk-inserts
# into the wide FundPrices table (1 row per market date).
# ~5,787 trading days back to June 2003.
# =============================================================================

param(
    [string]$ConnectionString = "Server=sql5111.site4now.net,1433;Database=db_a2a3b9_tspmasterprd;User Id=db_a2a3b9_tspmasterprd_admin;Password=@dm1n1str@t0r;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;"
)

$csvUrl = "https://www.tsp.gov/data/fund-price-history.csv"

$fundMap = [ordered]@{
    "G Fund"   = "GFund"
    "F Fund"   = "FFund"
    "C Fund"   = "CFund"
    "S Fund"   = "SFund"
    "I Fund"   = "IFund"
    "L Income" = "LIncome"
    "L 2030"   = "L2030"
    "L 2035"   = "L2035"
    "L 2040"   = "L2040"
    "L 2045"   = "L2045"
    "L 2050"   = "L2050"
    "L 2055"   = "L2055"
    "L 2060"   = "L2060"
    "L 2065"   = "L2065"
    "L 2070"   = "L2070"
    "L 2075"   = "L2075"
}

# -- 1. Download CSV -----------------------------------------------------------
Write-Host ""
Write-Host "[1/5] Downloading TSP fund price history from $csvUrl ..." -ForegroundColor Cyan
try {
    $resp = Invoke-WebRequest -Uri $csvUrl -UserAgent "Mozilla/5.0 (Windows NT 10.0; Win64; x64)" -UseBasicParsing -TimeoutSec 60
    $csvContent = [System.Text.Encoding]::UTF8.GetString($resp.Content)
    Write-Host "      Downloaded $("{0:N0}" -f $csvContent.Length) characters." -ForegroundColor Green
} catch {
    Write-Error "Failed to download CSV: $_"
    exit 1
}

# -- 2. Build DataTable Schema -------------------------------------------------
Write-Host "[2/5] Building DataTable Schema..." -ForegroundColor Cyan
$dt = New-Object System.Data.DataTable
[void]$dt.Columns.Add("Date", [string])

foreach ($colName in $fundMap.Values) {
    $col = New-Object System.Data.DataColumn($colName, [decimal])
    $col.AllowDBNull = $true
    [void]$dt.Columns.Add($col)
}

# -- 3. Parse CSV rows ---------------------------------------------------------
Write-Host "[3/5] Parsing CSV into daily rows..." -ForegroundColor Cyan
$lines  = $csvContent -split "\r?\n" | Where-Object { $_.Trim() -ne "" }
$header = ($lines[0] -split ",") | ForEach-Object { $_.Trim().Trim('"') }

[int]$rowCount = 0
foreach ($line in ($lines | Select-Object -Skip 1)) {
    $cols = $line -split ","
    if ($cols.Count -lt 2) { continue }

    $dateStr = $cols[0].Trim().Trim('"')
    $parsedDate = [datetime]::MinValue
    if (-not [datetime]::TryParse($dateStr, [ref]$parsedDate)) { continue }
    $dateOnly = $parsedDate.ToString("yyyy-MM-dd")
    if ([string]::IsNullOrWhiteSpace($dateOnly)) { continue }

    $row = $dt.NewRow()
    $row["Date"] = $dateOnly

    [bool]$hasAnyPrice = $false
    foreach ($csvFundName in $fundMap.Keys) {
        $colIdx = [Array]::IndexOf($header, $csvFundName)
        if ($colIdx -lt 0 -or $colIdx -ge $cols.Count) { continue }
        
        $priceStr = $cols[$colIdx].Trim().Trim('"')
        $price = 0.0
        if ([double]::TryParse($priceStr, [System.Globalization.NumberStyles]::Any, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$price)) {
            if ($price -gt 0) {
                $dbColName = $fundMap[$csvFundName]
                $row[$dbColName] = [decimal]$price
                $hasAnyPrice = $true
            }
        }
    }

    if ($hasAnyPrice) {
        [void]$dt.Rows.Add($row)
        $rowCount++
    }
}

Write-Host "      Parsed $("{0:N0}" -f $rowCount) daily rows across $("{0:N0}" -f ($lines.Count - 1)) trading days." -ForegroundColor Green

# -- 4. Connect to SQL Server --------------------------------------------------
Write-Host "[4/5] Connecting to SQL Server..." -ForegroundColor Cyan
Add-Type -AssemblyName "System.Data"
$conn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
try {
    $conn.Open()
    Write-Host "      Connected: $($conn.DataSource) / $($conn.Database)" -ForegroundColor Green
} catch {
    Write-Error "Connection failed: $_"
    exit 1
}

# Check existing
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT COUNT(*) FROM [FundPrices]"
$existing = [int]$cmd.ExecuteScalar()
Write-Host "      Existing rows in FundPrices: $("{0:N0}" -f $existing)" -ForegroundColor Yellow

if ($existing -gt 0) {
    Write-Host "      Truncating existing $existing rows before fresh import..." -ForegroundColor Yellow
    $truncCmd = $conn.CreateCommand()
    $truncCmd.CommandText = "TRUNCATE TABLE [FundPrices]"
    [void]$truncCmd.ExecuteNonQuery()
}

# Bulk copy
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$bulk = New-Object System.Data.SqlClient.SqlBulkCopy($conn)
$bulk.DestinationTableName = "[FundPrices]"
$bulk.BatchSize             = 5000
$bulk.BulkCopyTimeout       = 120

[void]$bulk.ColumnMappings.Add("Date", "Date")
foreach ($colName in $fundMap.Values) {
    [void]$bulk.ColumnMappings.Add($colName, $colName)
}

try {
    $bulk.WriteToServer($dt)
    $stopwatch.Stop()
    Write-Host ""
    Write-Host "SUCCESS: Inserted $("{0:N0}" -f $dt.Rows.Count) daily rows in $($stopwatch.Elapsed.TotalSeconds.ToString("F1")) seconds." -ForegroundColor Green
} catch {
    Write-Error "Bulk copy failed: $_"
    $conn.Close()
    exit 1
} finally {
    $bulk.Close()
    $conn.Close()
}

# -- 5. Verify -----------------------------------------------------------------
Write-Host ""
Write-Host "[5/5] Verifying row count & sample dates..." -ForegroundColor Cyan
$verifyConn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
$verifyConn.Open()
$verifyCmd = $verifyConn.CreateCommand()
$verifyCmd.CommandText = "SELECT COUNT(*) AS TotalDays, MIN(Date) AS EarliestDate, MAX(Date) AS LatestDate FROM [FundPrices]"
$reader = $verifyCmd.ExecuteReader()
if ($reader.Read()) {
    Write-Host "      Total Market Days : $("{0:N0}" -f $reader["TotalDays"])" -ForegroundColor White
    Write-Host "      Earliest Date     : $($reader["EarliestDate"])" -ForegroundColor White
    Write-Host "      Latest Date       : $($reader["LatestDate"])" -ForegroundColor White
}
$reader.Close()
$verifyConn.Close()

Write-Host ""
Write-Host "TSP historical data seed (Wide Schema) complete!" -ForegroundColor Green
