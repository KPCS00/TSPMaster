// Quick connection test using Microsoft.Data.SqlClient 6.x
// Run with: dotnet run --project TestConn
using Microsoft.Data.SqlClient;

var variants = new[]
{
    "Server=sql5111.site4now.net,1433;Database=db_a2a3b9_tspmasterprd;User Id=db_a2a3b9_tspmasterprd_admin;Password=@dm1n1str@t0r;TrustServerCertificate=True;Encrypt=Optional;Connect Timeout=15;",
    "Server=sql5111.site4now.net,1433;Database=db_a2a3b9_tspmasterprd;User Id=db_a2a3b9_tspmasterprd_admin;Password=@dm1n1str@t0r;TrustServerCertificate=True;Encrypt=false;Connect Timeout=15;",
    "Server=tcp:sql5111.site4now.net,1433;Database=db_a2a3b9_tspmasterprd;User Id=db_a2a3b9_tspmasterprd_admin;Password=@dm1n1str@t0r;TrustServerCertificate=True;Encrypt=Optional;Connect Timeout=15;",
};

foreach (var cs in variants)
{
    Console.Write($"Testing: {cs[..Math.Min(80, cs.Length)]}... ");
    try
    {
        using var conn = new SqlConnection(cs);
        await conn.OpenAsync();
        Console.WriteLine($"SUCCESS (State={conn.State})");
        conn.Close();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL: {ex.Message[..Math.Min(120, ex.Message.Length)]}");
    }
}
