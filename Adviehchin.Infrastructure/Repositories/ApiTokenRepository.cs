using Microsoft.EntityFrameworkCore;
using Adviehchin.Domain.Entities;
using Adviehchin.Domain.Interfaces;
using Adviehchin.Infrastructure.Data;

namespace Adviehchin.Infrastructure.Repositories;

public class ApiTokenRepository : IApiTokenRepository
{
    private readonly AdviehchinDbContext _context;

    public ApiTokenRepository(AdviehchinDbContext context) => _context = context;

    public Task<UserApiToken?> GetByHashAsync(string tokenHash) =>
        _context.ApiTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    public async Task AddAsync(UserApiToken token)
    {
        await _context.ApiTokens.AddAsync(token);
    }

    public Task<List<UserApiToken>> GetActiveForUserAsync(string userId) =>
        _context.ApiTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync();

    public Task<int> DeleteExpiredAsync(DateTime cutoffUtc) =>
        _context.ApiTokens
            .Where(t => (t.ExpiresAtUtc < cutoffUtc || t.IsRevoked) && t.CreatedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync();
}

public class LoginHistoryRepository : ILoginHistoryRepository
{
    private readonly AdviehchinDbContext _context;

    public LoginHistoryRepository(AdviehchinDbContext context) => _context = context;

    public async Task AddAsync(LoginHistory entry)
    {
        await _context.LoginHistories.AddAsync(entry);
    }

    public Task<List<LoginHistory>> GetRecentForUserAsync(string userId, int take = 20) =>
        _context.LoginHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.OccurredAtUtc)
            .Take(take)
            .ToListAsync();

    public Task<int> DeleteOlderThanAsync(DateTime cutoffUtc) =>
        _context.LoginHistories
            .Where(h => h.OccurredAtUtc < cutoffUtc)
            .ExecuteDeleteAsync();
}
