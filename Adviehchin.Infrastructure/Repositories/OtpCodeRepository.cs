using Microsoft.EntityFrameworkCore;
using Adviehchin.Domain.Entities;
using Adviehchin.Domain.Interfaces;
using Adviehchin.Infrastructure.Data;

namespace Adviehchin.Infrastructure.Repositories;

public class OtpCodeRepository : IOtpRepository
{
    private readonly AdviehchinDbContext _context;

    public OtpCodeRepository(AdviehchinDbContext context) => _context = context;

    public async Task AddAsync(OtpCode otp)
    {
        await _context.OtpCodes.AddAsync(otp);
        //await _context.SaveChangesAsync();
    }

    public async Task<OtpCode?> GetLatestValidAsync(string phoneNumber, string code, DateTime nowUtc)
    {
        return await _context.OtpCodes
            .Where(o => o.PhoneNumber == phoneNumber
                        && o.Code == code
                        && !o.IsUsed
                        && o.ExpiresAt > nowUtc)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task MarkAsUsedAsync(int id)
    {
        var otp = await _context.OtpCodes.FindAsync(id);
        if (otp is not null)
        {
            otp.IsUsed = true;
            //await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> TryMarkAsUsedAsync(int id)
    {
        var rows = await _context.OtpCodes
            .Where(o => o.Id == id && !o.IsUsed)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.IsUsed, true));
        return rows > 0;
    }

    public async Task InvalidateOthersAsync(string phoneNumber, int keepId, DateTime nowUtc)
    {
        await _context.OtpCodes
            .Where(o => o.PhoneNumber == phoneNumber && o.Id != keepId && !o.IsUsed && o.ExpiresAt > nowUtc)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.IsUsed, true));
    }

    public async Task<int> DeleteExpiredAsync(DateTime cutoffUtc)
        => await _context.OtpCodes
            .Where(o => o.ExpiresAt < cutoffUtc)
            .ExecuteDeleteAsync();
}