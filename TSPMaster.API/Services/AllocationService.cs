using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TSPMaster.API.Data;
using TSPMaster.API.Dtos.Allocations;
using TSPMaster.API.Models;

namespace TSPMaster.API.Services;

public class AllocationService : IAllocationService
{
    private readonly ApplicationDbContext _db;

    public AllocationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AllocationDto>> GetAllocationsAsync(string userId)
    {
        return await _db.FundAllocations
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.FundName)
            .Select(a => new AllocationDto(a.FundName, a.Percentage, a.UpdatedAt))
            .ToListAsync();
    }

    public async Task<TransferStatusDto> GetTransferStatusAsync(string userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var count = await _db.AllocationMoves
            .CountAsync(m => m.UserId == userId && m.MonthKey == currentMonth);

        var remaining = Math.Max(0, 3 - count);
        var isMove3GFundOnly = count == 2;

        return new TransferStatusDto(count, remaining, 3, isMove3GFundOnly, currentMonth);
    }

    public async Task SetAllocationsAsync(string userId, List<AllocationItem> allocations)
    {
        await RecordMoveAsync(userId, new RecordMoveRequest(
            DateTime.UtcNow,
            "Allocation Update",
            allocations,
            null
        ));
    }

    public async Task<AllocationOverviewDto> GetOverviewAsync(string userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var currentAllocations = await GetAllocationsAsync(userId);
        var transferStatus = await GetTransferStatusAsync(userId);
        var moveHistory = await GetMoveHistoryAsync(userId);

        // Fetch latest active recommendation if available
        var latestRecommendation = await _db.AnalysisResults
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.GeneratedAt)
            .FirstOrDefaultAsync();

        return new AllocationOverviewDto(
            user.InitialTspBalance,
            user.CurrentTspBalance,
            user.InitialBalanceDate,
            currentAllocations,
            transferStatus,
            moveHistory,
            latestRecommendation?.TopRecommendation,
            latestRecommendation?.RecommendationText
        );
    }

    public async Task SetInitialBalanceAsync(string userId, decimal balance, DateTime? effectiveDate)
    {
        if (balance < 0)
            throw new InvalidOperationException("Balance cannot be negative.");

        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        user.InitialTspBalance = balance;
        user.InitialBalanceDate = effectiveDate ?? DateTime.UtcNow;

        if (user.CurrentTspBalance == 0m || user.CurrentTspBalance < balance)
        {
            user.CurrentTspBalance = balance;
        }

        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task<AllocationMoveDto> RecordMoveAsync(string userId, RecordMoveRequest request)
    {
        if (request.Allocations is null || !request.Allocations.Any())
            throw new InvalidOperationException("At least one allocation is required.");

        var activeAllocations = request.Allocations.Where(a => a.Percentage > 0).ToList();
        var total = activeAllocations.Sum(a => a.Percentage);
        if (Math.Abs(total - 100m) > 0.01m)
            throw new InvalidOperationException($"Allocations must sum to 100%. Current total: {total}%");

        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var moveDate = request.EffectiveDate == default ? DateTime.UtcNow : request.EffectiveDate;
        var monthKey = moveDate.ToString("yyyy-MM");

        var existingMovesInMonth = await _db.AllocationMoves
            .Where(m => m.UserId == userId && m.MonthKey == monthKey)
            .CountAsync();

        if (existingMovesInMonth >= 3)
        {
            throw new InvalidOperationException($"You have used all 3 allowed Interfund Transfers (IFT) for {monthKey}. Moves reset on the 1st of next month.");
        }

        var moveNumber = existingMovesInMonth + 1;

        // 3rd Move Rule: Restricted to 100% G Fund
        if (moveNumber == 3)
        {
            bool isAllGFund = activeAllocations.Count == 1 &&
                              activeAllocations[0].FundName.Equals("G Fund", StringComparison.OrdinalIgnoreCase) &&
                              activeAllocations[0].Percentage == 100m;

            if (!isAllGFund)
            {
                throw new InvalidOperationException("Under TSP rules, your 3rd Interfund Transfer of the month must be 100% G Fund.");
            }
        }

        var balanceAtMove = request.UpdatedBalance ?? (user.CurrentTspBalance > 0 ? user.CurrentTspBalance : user.InitialTspBalance);
        if (request.UpdatedBalance.HasValue && request.UpdatedBalance.Value > 0)
        {
            user.CurrentTspBalance = request.UpdatedBalance.Value;
        }

        var json = JsonSerializer.Serialize(activeAllocations);

        var move = new AllocationMove
        {
            UserId = userId,
            EffectiveDate = moveDate,
            Description = string.IsNullOrWhiteSpace(request.Description) ? $"Move #{moveNumber} ({monthKey})" : request.Description,
            BalanceAtMove = balanceAtMove,
            AllocationsJson = json,
            MoveNumberInMonth = moveNumber,
            MonthKey = monthKey,
            CreatedAt = DateTime.UtcNow
        };

        _db.AllocationMoves.Add(move);

        // Update user's current allocations table
        var existingAllocations = await _db.FundAllocations.Where(a => a.UserId == userId).ToListAsync();
        _db.FundAllocations.RemoveRange(existingAllocations);

        var newAllocations = activeAllocations.Select(a => new FundAllocation
        {
            UserId = userId,
            FundName = a.FundName,
            Percentage = a.Percentage,
            UpdatedAt = moveDate
        });
        _db.FundAllocations.AddRange(newAllocations);

        // Update user monthly count stats
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        if (user.LastTransferMonth != currentMonth)
        {
            user.LastTransferMonth = currentMonth;
            user.MonthlyTransfersCount = currentMonth == monthKey ? moveNumber : 0;
        }
        else if (currentMonth == monthKey)
        {
            user.MonthlyTransfersCount = moveNumber;
        }

        _db.Users.Update(user);
        await _db.SaveChangesAsync();

        return new AllocationMoveDto(
            move.Id,
            move.EffectiveDate,
            move.Description,
            move.BalanceAtMove,
            activeAllocations,
            move.MoveNumberInMonth,
            move.MonthKey,
            move.CreatedAt
        );
    }

    public async Task DeleteMoveAsync(string userId, int moveId)
    {
        var move = await _db.AllocationMoves
            .FirstOrDefaultAsync(m => m.Id == moveId && m.UserId == userId)
            ?? throw new InvalidOperationException("Move entry not found.");

        var monthKey = move.MonthKey;
        _db.AllocationMoves.Remove(move);
        await _db.SaveChangesAsync();

        // Re-index move numbers for that month
        var remainingMovesInMonth = await _db.AllocationMoves
            .Where(m => m.UserId == userId && m.MonthKey == monthKey)
            .OrderBy(m => m.EffectiveDate)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync();

        for (int i = 0; i < remainingMovesInMonth.Count; i++)
        {
            remainingMovesInMonth[i].MoveNumberInMonth = i + 1;
        }
        _db.AllocationMoves.UpdateRange(remainingMovesInMonth);

        // Update current allocations to latest move if available
        var latestMove = await _db.AllocationMoves
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.EffectiveDate)
            .ThenByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestMove != null)
        {
            var active = JsonSerializer.Deserialize<List<AllocationItem>>(latestMove.AllocationsJson) ?? new();
            var existingAllocations = await _db.FundAllocations.Where(a => a.UserId == userId).ToListAsync();
            _db.FundAllocations.RemoveRange(existingAllocations);

            _db.FundAllocations.AddRange(active.Select(a => new FundAllocation
            {
                UserId = userId,
                FundName = a.FundName,
                Percentage = a.Percentage,
                UpdatedAt = latestMove.EffectiveDate
            }));
        }

        // Update user monthly transfers count
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
            if (user.LastTransferMonth == currentMonth)
            {
                user.MonthlyTransfersCount = await _db.AllocationMoves
                    .CountAsync(m => m.UserId == userId && m.MonthKey == currentMonth);
                _db.Users.Update(user);
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<AllocationMoveDto>> GetMoveHistoryAsync(string userId)
    {
        var moves = await _db.AllocationMoves
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.EffectiveDate)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();

        return moves.Select(m =>
        {
            var allocations = JsonSerializer.Deserialize<List<AllocationItem>>(m.AllocationsJson) ?? new();
            return new AllocationMoveDto(
                m.Id,
                m.EffectiveDate,
                m.Description,
                m.BalanceAtMove,
                allocations,
                m.MoveNumberInMonth,
                m.MonthKey,
                m.CreatedAt
            );
        }).ToList();
    }
}
