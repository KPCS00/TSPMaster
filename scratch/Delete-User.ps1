$connectionString = "Server=sql5111.site4now.net,1433;Database=db_a2a3b9_tspmasterprd;User Id=db_a2a3b9_tspmasterprd_admin;Password=@dm1n1str@t0r;TrustServerCertificate=True;Encrypt=False;Connection Timeout=30;"

Add-Type -Assembly "System.Data"

$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()

Write-Host "Connected to database successfully."

# Find users matching Ken Persky
$selectCmd = $conn.CreateCommand()
$selectCmd.CommandText = "SELECT Id, Email, FirstName, LastName FROM AspNetUsers WHERE FirstName LIKE '%Ken%' OR LastName LIKE '%Persky%' OR Email LIKE '%persky%' OR Email LIKE '%ken%'"

$reader = $selectCmd.ExecuteReader()
$matchingUsers = @()
while ($reader.Read()) {
    $matchingUsers += [PSCustomObject]@{
        Id        = $reader["Id"]
        Email     = $reader["Email"]
        FirstName = $reader["FirstName"]
        LastName  = $reader["LastName"]
    }
}
$reader.Close()

Write-Host "Found $($matchingUsers.Count) matching user(s):"
foreach ($u in $matchingUsers) {
    Write-Host " - Id: $($u.Id), Name: $($u.FirstName) $($u.LastName), Email: $($u.Email)"
}

if ($matchingUsers.Count -gt 0) {
    foreach ($u in $matchingUsers) {
        $userId = $u.Id
        Write-Host "Deleting user $($u.Email) ($userId)..."

        # Delete dependent records first
        $delAlloc = $conn.CreateCommand()
        $delAlloc.CommandText = "DELETE FROM FundAllocations WHERE UserId = '$userId'"
        $delAlloc.ExecuteNonQuery() | Out-Null

        $delRoles = $conn.CreateCommand()
        $delRoles.CommandText = "DELETE FROM AspNetUserRoles WHERE UserId = '$userId'"
        $delRoles.ExecuteNonQuery() | Out-Null

        $delClaims = $conn.CreateCommand()
        $delClaims.CommandText = "DELETE FROM AspNetUserClaims WHERE UserId = '$userId'"
        $delClaims.ExecuteNonQuery() | Out-Null

        $delLogins = $conn.CreateCommand()
        $delLogins.CommandText = "DELETE FROM AspNetUserLogins WHERE UserId = '$userId'"
        $delLogins.ExecuteNonQuery() | Out-Null

        $delTokens = $conn.CreateCommand()
        $delTokens.CommandText = "DELETE FROM AspNetUserTokens WHERE UserId = '$userId'"
        $delTokens.ExecuteNonQuery() | Out-Null

        # Delete from AspNetUsers
        $delUser = $conn.CreateCommand()
        $delUser.CommandText = "DELETE FROM AspNetUsers WHERE Id = '$userId'"
        $rows = $delUser.ExecuteNonQuery()
        Write-Host "Deleted $($rows) row(s) from AspNetUsers."
    }
} else {
    Write-Host "No user named Ken Persky found in the database."
}

$conn.Close()
