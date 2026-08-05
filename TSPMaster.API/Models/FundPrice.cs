namespace TSPMaster.API.Models;

/// <summary>
/// Stores daily closing share prices for all TSP funds in a single wide row per market date.
/// Primary key is <see cref="Date"/>.
/// Sourced from tsp.gov CSV.
/// </summary>
public class FundPrice
{
    /// <summary>Market closing date (Primary Key).</summary>
    public DateOnly Date { get; set; }

    // Core Funds
    public decimal? GFund { get; set; }
    public decimal? FFund { get; set; }
    public decimal? CFund { get; set; }
    public decimal? SFund { get; set; }
    public decimal? IFund { get; set; }

    // Lifecycle (L) Funds
    public decimal? LIncome { get; set; }
    public decimal? L2030 { get; set; }
    public decimal? L2035 { get; set; }
    public decimal? L2040 { get; set; }
    public decimal? L2045 { get; set; }
    public decimal? L2050 { get; set; }
    public decimal? L2055 { get; set; }
    public decimal? L2060 { get; set; }
    public decimal? L2065 { get; set; }
    public decimal? L2070 { get; set; }
    public decimal? L2075 { get; set; }

    /// <summary>
    /// Gets price by standardized fund name string.
    /// </summary>
    public decimal? GetPrice(string fundName) => fundName switch
    {
        "G Fund" or "G" => GFund,
        "F Fund" or "F" => FFund,
        "C Fund" or "C" => CFund,
        "S Fund" or "S" => SFund,
        "I Fund" or "I" => IFund,
        "L Income" => LIncome,
        "L 2030" => L2030,
        "L 2035" => L2035,
        "L 2040" => L2040,
        "L 2045" => L2045,
        "L 2050" => L2050,
        "L 2055" => L2055,
        "L 2060" => L2060,
        "L 2065" => L2065,
        "L 2070" => L2070,
        "L 2075" => L2075,
        _ => null
    };

    /// <summary>
    /// Sets price by standardized fund name string.
    /// </summary>
    public void SetPrice(string fundName, decimal? price)
    {
        switch (fundName)
        {
            case "G Fund" or "G": GFund = price; break;
            case "F Fund" or "F": FFund = price; break;
            case "C Fund" or "C": CFund = price; break;
            case "S Fund" or "S": SFund = price; break;
            case "I Fund" or "I": IFund = price; break;
            case "L Income": LIncome = price; break;
            case "L 2030": L2030 = price; break;
            case "L 2035": L2035 = price; break;
            case "L 2040": L2040 = price; break;
            case "L 2045": L2045 = price; break;
            case "L 2050": L2050 = price; break;
            case "L 2055": L2055 = price; break;
            case "L 2060": L2060 = price; break;
            case "L 2065": L2065 = price; break;
            case "L 2070": L2070 = price; break;
            case "L 2075": L2075 = price; break;
        }
    }
}
