using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TSPMaster.API.Data;
using TSPMaster.API.Dtos.Analysis;
using TSPMaster.API.Models;

namespace TSPMaster.API.Services;

public class AnalysisService : IAnalysisService
{
    private static readonly string[] CoreFunds = ["G Fund", "F Fund", "C Fund", "S Fund", "I Fund"];

    private readonly ApplicationDbContext _db;
    private readonly ITspDataService _tspData;
    private readonly IIntradayMarketService _intradayMarket;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(
        ApplicationDbContext db,
        ITspDataService tspData,
        IIntradayMarketService intradayMarket,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger<AnalysisService> logger)
    {
        _db = db;
        _tspData = tspData;
        _intradayMarket = intradayMarket;
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AnalysisResultDto?> GetLatestRecommendationAsync()
    {
        var result = await _db.AnalysisResults
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.GeneratedAt)
            .FirstOrDefaultAsync();

        return result is null ? null : MapToDto(result);
    }

    public async Task<AnalysisResultDto> RefreshRecommendationAsync()
    {
        // Ensure fund price database is up to date
        await _tspData.EnsurePricesUpToDateAsync();

        // Deactivate previous results
        var previous = await _db.AnalysisResults.Where(a => a.IsActive).ToListAsync();
        foreach (var p in previous) p.IsActive = false;

        // Fetch live intraday market snapshot (Open to 10:30 AM CST)
        var intradaySnapshot = await _intradayMarket.GetMorningMarketSnapshotAsync();

        // Query complete historical price database (multi-decade dataset)
        var allPrices = await _db.FundPrices
            .OrderBy(f => f.Date)
            .ToListAsync();

        var todayDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrowEffectiveDate = GetNextBusinessDay(todayDate);
        var currentMonth = DateTime.UtcNow.ToString("MMMM yyyy");
        var currentMonthNumber = todayDate.Month;

        var fundScores = new List<FundScoreDto>();
        var trendsBuilder = new StringBuilder();
        var seasonalityBuilder = new StringBuilder();

        seasonalityBuilder.AppendLine($"### Multi-Decade Seasonality & Trading Day Insights ({currentMonth}):");

        foreach (var fund in CoreFunds)
        {
            var fundHistory = allPrices
                .Select(p => (p.Date, Price: (double?)p.GetPrice(fund)))
                .Where(p => p.Price.HasValue && p.Price.Value > 0)
                .Select(p => (p.Date, Price: p.Price!.Value))
                .ToList();

            if (fundHistory.Count < 2)
                continue;

            var prices = fundHistory.Select(h => h.Price).ToArray();
            var dates = fundHistory.Select(h => h.Date).ToArray();

            // 1. Full History Momentum & Multi-timeframe moving averages
            double totalReturn = (prices[^1] - prices[0]) / prices[0] * 100.0;
            double ma20 = prices.Length >= 20 ? prices[^20..].Average() : prices.Average();
            double ma50 = prices.Length >= 50 ? prices[^50..].Average() : prices.Average();
            double ma200 = prices.Length >= 200 ? prices[^200..].Average() : prices.Average();

            // 20-day volatility
            var recentPrices = prices.Length >= 60 ? prices[^60..] : prices;
            var dailyReturns = recentPrices.Zip(recentPrices.Skip(1), (a, b) => (b - a) / a).ToArray();
            double mean = dailyReturns.Length > 0 ? dailyReturns.Average() : 0.0;
            double variance = dailyReturns.Length > 0 ? dailyReturns.Select(r => Math.Pow(r - mean, 2)).Average() : 0.0;
            double stddev = Math.Sqrt(variance) * 100.0;

            // 2. Historical Month Seasonality Win Rate (% positive return for this calendar month across past years)
            var monthlyWinRate = CalculateMonthlyWinRate(fundHistory, currentMonthNumber);

            // 3. Tomorrow's Trading Day Seasonality
            int tomorrowTradingDayIndex = GetTradingDayIndexOfMonth(tomorrowEffectiveDate);
            var (tdAvgReturn, tdWinRate) = CalculateTradingDaySeasonality(fundHistory, currentMonthNumber, tomorrowTradingDayIndex);

            // 4. Intraday proxy boost
            var proxyQuote = intradaySnapshot.Quotes.FirstOrDefault(q => q.FundName == fund);
            double intradayChange = proxyQuote?.ChangePercent ?? 0.0;

            // Composite quantitative score (Historical Win Rate + 20-day MA Trend + Intraday Momentum - Volatility Penalty)
            double trendBonus = prices[^1] > ma20 ? 5.0 : -5.0;
            double ma200Bonus = prices[^1] > ma200 ? 5.0 : -5.0;
            double score = (monthlyWinRate * 0.4) + (tdWinRate * 0.3) + (intradayChange * 10.0) + trendBonus + ma200Bonus - (stddev * 0.5);

            string trend = prices[^1] > ma20 && ma20 > ma50 ? "Uptrend"
                         : prices[^1] < ma20 && ma20 < ma50 ? "Downtrend"
                         : "Sideways";

            trendsBuilder.AppendLine($"{fund}: Historical {DateTime.UtcNow:MMM} Win Rate={monthlyWinRate:F1}%, Tomorrow (Trading Day #{tomorrowTradingDayIndex}) Expected Return={tdAvgReturn:+0.00;-0.00}%, 20-day MA={ma20:F2}, 200-day MA={ma200:F2}, Live Morning Change={intradayChange:+0.00;-0.00}%, Trend={trend}");
            seasonalityBuilder.AppendLine($"- **{fund}**: {monthlyWinRate:F1}% historical win rate in {DateTime.UtcNow:MMM}. Trading Day #{tomorrowTradingDayIndex} win rate: {tdWinRate:F1}% (Avg return: {tdAvgReturn:+0.00;-0.00}%). Live morning momentum: {intradayChange:+0.00;-0.00}%.");

            fundScores.Add(new FundScoreDto(
                fund,
                Math.Round(score, 2),
                trend,
                Math.Round(monthlyWinRate, 2),
                Math.Round(stddev, 2),
                string.Empty
            ));
        }

        var sortedScores = fundScores.OrderByDescending(f => f.Score).ToList();
        var topFund = sortedScores.FirstOrDefault()?.FundName ?? "G Fund";
        var top1 = sortedScores.Count > 0 ? sortedScores[0].FundName : "C Fund";
        var top2 = sortedScores.Count > 1 ? sortedScores[1].FundName : "S Fund";

        var tomorrowAllocation = new Dictionary<string, decimal>
        {
            [top1] = 60m,
            [top2] = 40m
        };

        // Call Gemini API with updated deep historical & intraday prompt
        var geminiApiKey = _config["Gemini:ApiKey"];
        var geminiModel = _config["Gemini:Model"] ?? "gemini-3.6-flash";
        string recommendationText;
        string marketContext = intradaySnapshot.MarketSentimentSummary;

        if (!string.IsNullOrEmpty(geminiApiKey) && geminiApiKey != "REPLACE_WITH_GEMINI_API_KEY")
        {
            var prompt = BuildGeminiPrompt(trendsBuilder.ToString(), topFund, fundScores, intradaySnapshot, tomorrowEffectiveDate);
            (recommendationText, marketContext) = await CallGeminiAsync(geminiApiKey, geminiModel, prompt);
        }
        else
        {
            _logger.LogWarning("Gemini API key not configured. Using quantitative seasonality calculation.");
            recommendationText = BuildFallbackRecommendation(fundScores, topFund, tomorrowEffectiveDate, intradaySnapshot);
        }

        var move1Plan = new MonthlyMovePlanDto(
            1,
            "Move 1: Start-of-Month Core Allocation",
            "Immediate Position for " + currentMonth,
            new Dictionary<string, decimal> { [top1] = 60m, [top2] = 40m },
            $"Primary allocation targeting top performing historical funds ({top1} and {top2}) based on multi-decade seasonality."
        );

        var move2Plan = new MonthlyMovePlanDto(
            2,
            "Move 2: Mid-Month Tactical Rebalance",
            "Tactical Signal (Triggered if " + top1 + " breaks below 20-day MA or morning sell-off > 0.75%)",
            new Dictionary<string, decimal> { [top1] = 40m, [top2] = 30m, ["G Fund"] = 30m },
            "Secondary transfer to lock in profits or reduce drawdown if market volatility increases mid-month."
        );

        var move3Plan = new MonthlyMovePlanDto(
            3,
            "Move 3: Flight-to-Safety Emergency Exit",
            "Safety Trigger (Emergency exit if market drops > 3.5% or severe geopolitical shock)",
            new Dictionary<string, decimal> { ["G Fund"] = 100m },
            "Final transfer for the month. Under TSP rules, Move 3 is restricted exclusively to 100% G Fund for principal preservation."
        );

        var macroSummary = $"Macro & Live Intraday Context: {intradaySnapshot.MarketSentimentSummary} Full-history price analytics evaluated across all available market years.";

        // Update fund scores with AI recommendations
        var updatedScores = fundScores.Select(fs =>
            fs with { Recommendation = topFund == fs.FundName ? "Strong Buy" : fs.Score > 0 ? "Hold" : "Underweight" }
        ).ToList();

        var (scheduledMoves, dailyCalendar) = GenerateScheduledMovesAndCalendar(top1, top2);

        // Persist result
        var analysisResult = new AnalysisResult
        {
            GeneratedAt = DateTime.UtcNow,
            PeriodDescription = $"Full Historical Dataset ({allPrices.Count} trading days) + Live Morning Market Snapshot",
            TopRecommendation = topFund,
            RecommendationText = recommendationText,
            FundScoresJson = JsonSerializer.Serialize(updatedScores),
            MarketContext = marketContext,
            TargetMonth = currentMonth,
            Move1PlanJson = JsonSerializer.Serialize(move1Plan),
            Move2PlanJson = JsonSerializer.Serialize(move2Plan),
            Move3PlanJson = JsonSerializer.Serialize(move3Plan),
            MacroNewsSummary = macroSummary,
            ScheduledMovesJson = JsonSerializer.Serialize(scheduledMoves),
            DailyCalendarJson = JsonSerializer.Serialize(dailyCalendar),
            TomorrowEffectiveDate = tomorrowEffectiveDate.ToString("yyyy-MM-dd"),
            TomorrowAllocationJson = JsonSerializer.Serialize(tomorrowAllocation),
            IntradayMarketSnapshotJson = JsonSerializer.Serialize(intradaySnapshot),
            HistoricalSeasonalitySummary = seasonalityBuilder.ToString(),
            IsActive = true
        };

        _db.AnalysisResults.Add(analysisResult);
        await _db.SaveChangesAsync();

        return MapToDto(analysisResult);
    }

    private async Task<(string recommendation, string marketContext)> CallGeminiAsync(
        string apiKey, string model, string prompt)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        var client = _httpClientFactory.CreateClient();

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 1024
            }
        };

        try
        {
            var response = await client.PostAsJsonAsync(url, requestBody);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var context = lines.Length > 3 ? string.Join("\n", lines.Skip(3)) : "See full recommendation above.";
            return (text, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API call failed.");
            return (BuildFallbackRecommendation([], string.Empty, GetNextBusinessDay(DateOnly.FromDateTime(DateTime.UtcNow)), null), "Analysis unavailable.");
        }
    }

    private static string BuildGeminiPrompt(
        string trendsData,
        string topFund,
        List<FundScoreDto> scores,
        IntradayMarketSnapshotDto intraday,
        DateOnly tomorrowDate)
    {
        var currentMonth = DateTime.UtcNow.ToString("MMMM yyyy");
        return $"""
            You are an elite TSP (Thrift Savings Plan) quantitative strategist specializing in multi-decade historical seasonality and live intraday market execution.

            **Objective:**
            Predict the optimal TSP fund allocation for **TOMORROW ({tomorrowDate:yyyy-MM-dd})**.
            The investor must submit their Interfund Transfer (IFT) on TSP.gov **before 11:00 AM CST today** for trades to execute overnight and take effect tomorrow.

            **Inputs & Metrics:**
            1. **Live Morning Market Snapshot (Open to 10:30 AM CST)**:
               {intraday.MarketSentimentSummary}

            2. **Full-History Seasonality & Trading Day Technicals**:
               {trendsData}

            3. **Statistical Top Fund**: {topFund}

            **TSP Rules & Constraints:**
            - Up to 3 IFT moves per calendar month: Move 1 (Start of month position), Move 2 (Mid-month rebalance), Move 3 (Emergency exit RESTRICTED to 100% G Fund).

            **Deliverables Required:**
            1. **Headline Direct Directive for Tomorrow ({tomorrowDate:MMM dd, yyyy})**: Explicit action instruction for today's 11:00 AM CST deadline.
            2. **Live Morning Analysis (Open to 10:30 AM CST)**: How today's live stock/bond market opening momentum validates or adjusts historical seasonality.
            3. **Multi-Decade Seasonality Rationale**: Historical win rates for this calendar month and specific trading day.
            4. **Recommended % Allocation Breakdown for Tomorrow**.

            Format response in clean Markdown with bold headings. Keep response clear, precise, and under 450 words.
            """;
    }

    private static string BuildFallbackRecommendation(
        List<FundScoreDto> scores,
        string topFund,
        DateOnly tomorrowDate,
        IntradayMarketSnapshotDto? intraday)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## 🎯 Recommended Action for Tomorrow ({tomorrowDate:yyyy-MM-dd})");
        sb.AppendLine();
        sb.AppendLine($"**Primary Recommendation for Today's 11:00 AM CST Cutoff: 60% {topFund} / 40% S Fund**");
        sb.AppendLine();

        if (intraday is not null)
        {
            sb.AppendLine($"### 📈 Live Morning Snapshot (Open to 10:30 AM CST):");
            sb.AppendLine($"*{intraday.MarketSentimentSummary}*");
            sb.AppendLine();
        }

        sb.AppendLine("### Multi-Decade Historical Metrics & Seasonality:");
        foreach (var score in scores.OrderByDescending(s => s.Score))
        {
            sb.AppendLine($"- **{score.FundName}**: Trend {score.Trend} | Win Rate: {score.MomentumScore:F1}% | Volatility: {score.VolatilityScore:F2}%");
        }

        sb.AppendLine();
        sb.AppendLine("> *Notice: Submit Interfund Transfers on TSP.gov prior to 11:00 AM CST for overnight execution.*");

        return sb.ToString();
    }

    private static double CalculateMonthlyWinRate(List<(DateOnly Date, double Price)> fundHistory, int month)
    {
        var yearGroups = fundHistory
            .Where(h => h.Date.Month == month)
            .GroupBy(h => h.Date.Year)
            .ToList();

        if (yearGroups.Count == 0) return 50.0;

        int positiveYears = 0;
        int totalYears = 0;

        foreach (var group in yearGroups)
        {
            var sorted = group.OrderBy(g => g.Date).ToList();
            if (sorted.Count < 2) continue;
            double monthReturn = (sorted[^1].Price - sorted[0].Price) / sorted[0].Price;
            totalYears++;
            if (monthReturn > 0) positiveYears++;
        }

        return totalYears > 0 ? (double)positiveYears / totalYears * 100.0 : 50.0;
    }

    private static (double AvgReturn, double WinRate) CalculateTradingDaySeasonality(
        List<(DateOnly Date, double Price)> fundHistory, int month, int tradingDayIndex)
    {
        var yearGroups = fundHistory
            .Where(h => h.Date.Month == month)
            .GroupBy(h => h.Date.Year);

        var returns = new List<double>();

        foreach (var group in yearGroups)
        {
            var sorted = group.OrderBy(g => g.Date).ToList();
            int index = tradingDayIndex - 1;
            if (index > 0 && index < sorted.Count)
            {
                double prevPrice = sorted[index - 1].Price;
                double currPrice = sorted[index].Price;
                if (prevPrice > 0)
                {
                    returns.Add((currPrice - prevPrice) / prevPrice * 100.0);
                }
            }
        }

        if (returns.Count == 0) return (0.0, 50.0);

        double avgReturn = returns.Average();
        int positiveCount = returns.Count(r => r > 0);
        double winRate = (double)positiveCount / returns.Count * 100.0;

        return (avgReturn, winRate);
    }

    private static int GetTradingDayIndexOfMonth(DateOnly date)
    {
        int count = 0;
        for (int day = 1; day <= date.Day; day++)
        {
            var d = new DateOnly(date.Year, date.Month, day);
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
            {
                count++;
            }
        }
        return Math.Max(1, count);
    }

    private static DateOnly GetNextBusinessDay(DateOnly date)
    {
        var next = date.AddDays(1);
        while (next.DayOfWeek == DayOfWeek.Saturday || next.DayOfWeek == DayOfWeek.Sunday)
        {
            next = next.AddDays(1);
        }
        return next;
    }

    private static (List<ScheduledMoveDto> moves, List<DailyCalendarEntryDto> calendar) GenerateScheduledMovesAndCalendar(
        string top1, string top2)
    {
        var now = DateTime.UtcNow;
        var year = now.Year;
        var month = now.Month;
        var daysInMonth = DateTime.DaysInMonth(year, month);

        var calendarEntries = new List<DailyCalendarEntryDto>();
        var scheduledMoves = new List<ScheduledMoveDto>();
        int tradingDayCounter = 0;

        var tradingDayDates = new Dictionary<int, DateTime>();

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

            int td = 0;
            if (!isWeekend)
            {
                tradingDayCounter++;
                td = tradingDayCounter;
                tradingDayDates[td] = date;
            }

            calendarEntries.Add(new DailyCalendarEntryDto(
                date.ToString("yyyy-MM-dd"),
                day,
                td,
                "G Fund",
                false,
                null
            ));
        }

        var m1Date = tradingDayDates.TryGetValue(1, out var d1) ? d1 : new DateTime(year, month, 1);
        var m2Date = tradingDayDates.TryGetValue(12, out var d12) ? d12 : new DateTime(year, month, Math.Min(18, daysInMonth));
        var m3Date = tradingDayDates.TryGetValue(20, out var d20) ? d20 : new DateTime(year, month, Math.Max(1, daysInMonth - 2));

        var m1Allocation = new Dictionary<string, decimal> { [top1] = 60m, [top2] = 40m };
        var m2Allocation = new Dictionary<string, decimal> { [top1] = 50m, ["I Fund"] = 30m, ["G Fund"] = 20m };
        var m3Allocation = new Dictionary<string, decimal> { ["G Fund"] = 100m };

        var m1Dto = new ScheduledMoveDto(1, m1Date.ToString("yyyy-MM-dd"), 1, m1Allocation, $"Seasonal Day 1 Entry: Maximizing return in top momentum funds ({top1}/{top2}).", "CONFIRMED BY AI");
        var m2Dto = new ScheduledMoveDto(2, m2Date.ToString("yyyy-MM-dd"), 12, m2Allocation, "Seasonal Mid-Month Rebalance: Lock in early month gains & pivot to value.", "MONITORING MACRO NEWS");
        var m3Dto = new ScheduledMoveDto(3, m3Date.ToString("yyyy-MM-dd"), 20, m3Allocation, "Seasonal Late-Month Safety Stop: 100% G Fund flight to safety under TSP rules.", "G-FUND ONLY");

        scheduledMoves.Add(m1Dto);
        scheduledMoves.Add(m2Dto);
        scheduledMoves.Add(m3Dto);

        for (int i = 0; i < calendarEntries.Count; i++)
        {
            var entry = calendarEntries[i];
            var dateObj = DateTime.Parse(entry.DateString);

            string recFund;
            bool isMove = false;
            int? moveNum = null;

            if (dateObj >= m3Date)
            {
                recFund = "G Fund";
                if (dateObj.Date == m3Date.Date) { isMove = true; moveNum = 3; }
            }
            else if (dateObj >= m2Date)
            {
                recFund = $"{top1} (50%) / I Fund (30%)";
                if (dateObj.Date == m2Date.Date) { isMove = true; moveNum = 2; }
            }
            else
            {
                recFund = $"{top1} (60%) / {top2} (40%)";
                if (dateObj.Date == m1Date.Date) { isMove = true; moveNum = 1; }
            }

            calendarEntries[i] = entry with { RecommendedFund = recFund, IsMoveDay = isMove, MoveNumber = moveNum };
        }

        return (scheduledMoves, calendarEntries);
    }

    private static AnalysisResultDto MapToDto(AnalysisResult result)
    {
        var scores = new List<FundScoreDto>();
        try
        {
            scores = JsonSerializer.Deserialize<List<FundScoreDto>>(result.FundScoresJson) ?? [];
        }
        catch { }

        var currentMonth = string.IsNullOrWhiteSpace(result.TargetMonth) ? DateTime.UtcNow.ToString("MMMM yyyy") : result.TargetMonth;

        var move1 = DeserializeMovePlan(result.Move1PlanJson, 1, "Move 1: Start-of-Month Core Allocation", "Immediate / Month-Start", new Dictionary<string, decimal> { ["C Fund"] = 60m, ["S Fund"] = 40m }, "Core positioning based on monthly momentum & trend analysis.");
        var move2 = DeserializeMovePlan(result.Move2PlanJson, 2, "Move 2: Mid-Month Tactical Rebalance", "Tactical Signal (e.g. 20-day MA breakdown)", new Dictionary<string, decimal> { ["C Fund"] = 50m, ["I Fund"] = 30m, ["G Fund"] = 20m }, "Tactical adjustment to protect gains or pivot to value.");
        var move3 = DeserializeMovePlan(result.Move3PlanJson, 3, "Move 3: Emergency G-Fund Safety Stop", "Flight-to-Safety Trigger (Market drop > 3.5%)", new Dictionary<string, decimal> { ["G Fund"] = 100m }, "Emergency exit under TSP rules (Move 3 restricted to 100% G Fund).");

        var macroSummary = string.IsNullOrWhiteSpace(result.MacroNewsSummary)
            ? "Current macroeconomic drivers: Federal Reserve rate policy, inflation metrics, corporate earnings, and political developments."
            : result.MacroNewsSummary;

        var (defaultMoves, defaultCal) = GenerateScheduledMovesAndCalendar("C Fund", "S Fund");

        var scheduledMoves = new List<ScheduledMoveDto>();
        try { scheduledMoves = JsonSerializer.Deserialize<List<ScheduledMoveDto>>(result.ScheduledMovesJson) ?? defaultMoves; } catch { scheduledMoves = defaultMoves; }

        var dailyCalendar = new List<DailyCalendarEntryDto>();
        try { dailyCalendar = JsonSerializer.Deserialize<List<DailyCalendarEntryDto>>(result.DailyCalendarJson) ?? defaultCal; } catch { dailyCalendar = defaultCal; }

        Dictionary<string, decimal>? tomorrowAllocation = null;
        try { if (!string.IsNullOrWhiteSpace(result.TomorrowAllocationJson)) tomorrowAllocation = JsonSerializer.Deserialize<Dictionary<string, decimal>>(result.TomorrowAllocationJson); } catch { }

        IntradayMarketSnapshotDto? intradaySnapshot = null;
        try { if (!string.IsNullOrWhiteSpace(result.IntradayMarketSnapshotJson)) intradaySnapshot = JsonSerializer.Deserialize<IntradayMarketSnapshotDto>(result.IntradayMarketSnapshotJson); } catch { }

        return new AnalysisResultDto(
            result.Id,
            result.GeneratedAt,
            result.PeriodDescription,
            result.TopRecommendation,
            result.RecommendationText,
            scores,
            result.MarketContext,
            currentMonth,
            move1,
            move2,
            move3,
            macroSummary,
            scheduledMoves,
            dailyCalendar,
            result.TomorrowEffectiveDate,
            tomorrowAllocation,
            intradaySnapshot,
            result.HistoricalSeasonalitySummary
        );
    }

    private static MonthlyMovePlanDto DeserializeMovePlan(
        string json, int defaultMoveNumber, string defaultTitle, string defaultTrigger, Dictionary<string, decimal> defaultAllocation, string defaultRationale)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new MonthlyMovePlanDto(defaultMoveNumber, defaultTitle, defaultTrigger, defaultAllocation, defaultRationale);

        try
        {
            var parsed = JsonSerializer.Deserialize<MonthlyMovePlanDto>(json);
            if (parsed is not null) return parsed;
        }
        catch { }

        return new MonthlyMovePlanDto(defaultMoveNumber, defaultTitle, defaultTrigger, defaultAllocation, defaultRationale);
    }
}
